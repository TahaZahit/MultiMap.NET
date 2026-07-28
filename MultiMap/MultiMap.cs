using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace MultiMaps
{
    /// <summary>
    /// A thread-safe, mutable map holding multiple values per key.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Reads take a shared lock and writes take an exclusive one, so concurrent readers never block
    /// each other. Every collection this type hands back is a snapshot taken under the lock, which
    /// means callers can enumerate results while other threads keep mutating the map.
    /// </para>
    /// <para>
    /// Values keep their insertion order under each key, in both duplicate modes. When
    /// <see cref="AllowsDuplicateValues"/> is <c>false</c>, each <see cref="Add"/> scans the key's
    /// existing values to reject repeats; that scan is linear in the number of values under that
    /// one key.
    /// </para>
    /// </remarks>
    /// <typeparam name="TKey">Type of the keys.</typeparam>
    /// <typeparam name="TValue">Type of the values.</typeparam>
    public class MultiMap<TKey, TValue> : IMultiMap<TKey, TValue>, IDisposable
    {
        private readonly Dictionary<TKey, List<TValue>> _map;
        private readonly IEqualityComparer<TValue> _valueComparer;
        private readonly ReaderWriterLockSlim _lock =
            new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

        private int _valueCount;
        private bool _disposed;

        /// <summary>
        /// Creates an empty map that allows duplicate values under a key.
        /// </summary>
        public MultiMap()
            : this(true, null, null)
        {
        }

        /// <summary>
        /// Creates an empty map.
        /// </summary>
        /// <param name="allowDuplicateValues">
        /// When <c>false</c>, adding a value already present under that key is rejected.
        /// </param>
        public MultiMap(bool allowDuplicateValues)
            : this(allowDuplicateValues, null, null)
        {
        }

        /// <summary>
        /// Creates an empty map with explicit comparers.
        /// </summary>
        /// <param name="allowDuplicateValues">
        /// When <c>false</c>, adding a value already present under that key is rejected.
        /// </param>
        /// <param name="keyComparer">Comparer for keys, or <c>null</c> for the default.</param>
        /// <param name="valueComparer">Comparer for values, or <c>null</c> for the default.</param>
        public MultiMap(
            bool allowDuplicateValues,
            IEqualityComparer<TKey> keyComparer,
            IEqualityComparer<TValue> valueComparer)
        {
            AllowsDuplicateValues = allowDuplicateValues;
            _valueComparer = valueComparer ?? EqualityComparer<TValue>.Default;
            _map = new Dictionary<TKey, List<TValue>>(keyComparer ?? EqualityComparer<TKey>.Default);
        }

        /// <summary>
        /// Creates a map populated from existing pairs.
        /// </summary>
        /// <param name="pairs">Pairs to copy in.</param>
        /// <param name="allowDuplicateValues">
        /// When <c>false</c>, repeated pairs in <paramref name="pairs"/> are collapsed to one.
        /// </param>
        public MultiMap(IEnumerable<KeyValuePair<TKey, TValue>> pairs, bool allowDuplicateValues = true)
            : this(allowDuplicateValues, null, null)
        {
            if (pairs == null) throw new ArgumentNullException(nameof(pairs));

            foreach (var pair in pairs)
            {
                AddCore(pair.Key, pair.Value);
            }
        }

        /// <inheritdoc />
        public bool AllowsDuplicateValues { get; }

        /// <summary>
        /// Number of distinct keys.
        /// </summary>
        /// <remarks>
        /// This is the <see cref="ILookup{TKey, TElement}"/> definition of <c>Count</c>. For the
        /// number of stored pairs use <see cref="ValueCount"/>.
        /// </remarks>
        public int Count
        {
            get
            {
                _lock.EnterReadLock();
                try { return _map.Count; }
                finally { _lock.ExitReadLock(); }
            }
        }

        /// <inheritdoc />
        public int ValueCount
        {
            get
            {
                _lock.EnterReadLock();
                try { return _valueCount; }
                finally { _lock.ExitReadLock(); }
            }
        }

        /// <inheritdoc />
        public IReadOnlyCollection<TKey> Keys
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    var keys = new TKey[_map.Count];
                    _map.Keys.CopyTo(keys, 0);
                    return keys;
                }
                finally { _lock.ExitReadLock(); }
            }
        }

        /// <inheritdoc />
        public IReadOnlyCollection<TValue> Values
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    var values = new List<TValue>(_valueCount);
                    foreach (var bucket in _map.Values)
                    {
                        values.AddRange(bucket);
                    }
                    return values;
                }
                finally { _lock.ExitReadLock(); }
            }
        }

        /// <summary>
        /// Values stored under <paramref name="key"/>, or an empty sequence when the key is absent.
        /// </summary>
        /// <remarks>
        /// Never throws on a missing key, matching <see cref="ILookup{TKey, TElement}"/> rather than
        /// <see cref="Dictionary{TKey, TValue}"/>.
        /// </remarks>
        public IEnumerable<TValue> this[TKey key]
        {
            get
            {
                _lock.EnterReadLock();
                try
                {
                    return _map.TryGetValue(key, out var bucket)
                        ? bucket.ToArray()
                        : Array.Empty<TValue>();
                }
                finally { _lock.ExitReadLock(); }
            }
        }

        /// <inheritdoc />
        public bool Add(TKey key, TValue value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            _lock.EnterWriteLock();
            try { return AddCore(key, value); }
            finally { _lock.ExitWriteLock(); }
        }

        /// <inheritdoc />
        public int AddRange(TKey key, IEnumerable<TValue> values)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
            if (values == null) throw new ArgumentNullException(nameof(values));

            // Materialize before locking so a lazy or slow sequence cannot stall other threads.
            var incoming = values as IList<TValue> ?? values.ToList();

            var added = 0;
            _lock.EnterWriteLock();
            try
            {
                for (var i = 0; i < incoming.Count; i++)
                {
                    if (AddCore(key, incoming[i])) added++;
                }
            }
            finally { _lock.ExitWriteLock(); }

            return added;
        }

        /// <inheritdoc />
        public bool Remove(TKey key, TValue value)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            _lock.EnterWriteLock();
            try
            {
                if (!_map.TryGetValue(key, out var bucket)) return false;

                var index = IndexOf(bucket, value);
                if (index < 0) return false;

                bucket.RemoveAt(index);
                _valueCount--;

                if (bucket.Count == 0) _map.Remove(key);
                return true;
            }
            finally { _lock.ExitWriteLock(); }
        }

        /// <inheritdoc />
        public int RemoveAll(TKey key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            _lock.EnterWriteLock();
            try
            {
                if (!_map.TryGetValue(key, out var bucket)) return 0;

                var removed = bucket.Count;
                _map.Remove(key);
                _valueCount -= removed;
                return removed;
            }
            finally { _lock.ExitWriteLock(); }
        }

        /// <inheritdoc />
        public void Clear()
        {
            _lock.EnterWriteLock();
            try
            {
                _map.Clear();
                _valueCount = 0;
            }
            finally { _lock.ExitWriteLock(); }
        }

        /// <inheritdoc />
        public bool ContainsKey(TKey key)
        {
            if (key == null) return false;

            _lock.EnterReadLock();
            try { return _map.ContainsKey(key); }
            finally { _lock.ExitReadLock(); }
        }

        /// <summary>
        /// Whether <paramref name="key"/> holds at least one value.
        /// </summary>
        /// <remarks>The <see cref="ILookup{TKey, TElement}"/> spelling of <see cref="ContainsKey"/>.</remarks>
        public bool Contains(TKey key) => ContainsKey(key);

        /// <inheritdoc />
        public bool ContainsPair(TKey key, TValue value)
        {
            if (key == null) return false;

            _lock.EnterReadLock();
            try
            {
                return _map.TryGetValue(key, out var bucket) && IndexOf(bucket, value) >= 0;
            }
            finally { _lock.ExitReadLock(); }
        }

        /// <inheritdoc />
        public bool ContainsValue(TValue value)
        {
            _lock.EnterReadLock();
            try
            {
                foreach (var bucket in _map.Values)
                {
                    if (IndexOf(bucket, value) >= 0) return true;
                }
                return false;
            }
            finally { _lock.ExitReadLock(); }
        }

        /// <inheritdoc />
        public bool TryGetValues(TKey key, out IReadOnlyList<TValue> values)
        {
            if (key == null)
            {
                values = null;
                return false;
            }

            _lock.EnterReadLock();
            try
            {
                if (_map.TryGetValue(key, out var bucket))
                {
                    values = bucket.ToArray();
                    return true;
                }

                values = null;
                return false;
            }
            finally { _lock.ExitReadLock(); }
        }

        /// <inheritdoc />
        public IEnumerable<KeyValuePair<TKey, TValue>> Pairs()
        {
            _lock.EnterReadLock();
            try
            {
                var pairs = new List<KeyValuePair<TKey, TValue>>(_valueCount);
                foreach (var entry in _map)
                {
                    foreach (var value in entry.Value)
                    {
                        pairs.Add(new KeyValuePair<TKey, TValue>(entry.Key, value));
                    }
                }
                return pairs;
            }
            finally { _lock.ExitReadLock(); }
        }

        /// <inheritdoc />
        public Dictionary<TKey, List<TValue>> ToDictionary()
        {
            _lock.EnterReadLock();
            try
            {
                var copy = new Dictionary<TKey, List<TValue>>(_map.Count, _map.Comparer);
                foreach (var entry in _map)
                {
                    copy[entry.Key] = new List<TValue>(entry.Value);
                }
                return copy;
            }
            finally { _lock.ExitReadLock(); }
        }

        /// <summary>
        /// Enumerates a snapshot of the map as one <see cref="IGrouping{TKey, TElement}"/> per key.
        /// </summary>
        public IEnumerator<IGrouping<TKey, TValue>> GetEnumerator()
        {
            _lock.EnterReadLock();
            List<IGrouping<TKey, TValue>> snapshot;
            try
            {
                snapshot = new List<IGrouping<TKey, TValue>>(_map.Count);
                foreach (var entry in _map)
                {
                    snapshot.Add(new Grouping(entry.Key, entry.Value.ToArray()));
                }
            }
            finally { _lock.ExitReadLock(); }

            return snapshot.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Releases the internal lock.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the internal lock.
        /// </summary>
        /// <param name="disposing">Whether managed resources should be released.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing) _lock.Dispose();
            _disposed = true;
        }

        /// <summary>
        /// Adds a pair. The caller must already hold the write lock.
        /// </summary>
        private bool AddCore(TKey key, TValue value)
        {
            if (!_map.TryGetValue(key, out var bucket))
            {
                bucket = new List<TValue>(1);
                _map[key] = bucket;
            }
            else if (!AllowsDuplicateValues && IndexOf(bucket, value) >= 0)
            {
                return false;
            }

            bucket.Add(value);
            _valueCount++;
            return true;
        }

        private int IndexOf(List<TValue> bucket, TValue value)
        {
            for (var i = 0; i < bucket.Count; i++)
            {
                if (_valueComparer.Equals(bucket[i], value)) return i;
            }
            return -1;
        }

        private sealed class Grouping : IGrouping<TKey, TValue>
        {
            private readonly TValue[] _values;

            internal Grouping(TKey key, TValue[] values)
            {
                Key = key;
                _values = values;
            }

            public TKey Key { get; }

            public IEnumerator<TValue> GetEnumerator() => ((IEnumerable<TValue>)_values).GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => _values.GetEnumerator();
        }
    }
}
