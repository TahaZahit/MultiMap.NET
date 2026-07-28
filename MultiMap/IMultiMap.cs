using System.Collections.Generic;
using System.Linq;

namespace MultiMaps
{
    /// <summary>
    /// A mutable map that can hold more than one value per key.
    /// </summary>
    /// <remarks>
    /// .NET ships <see cref="ILookup{TKey, TElement}"/> (via <c>Enumerable.ToLookup</c>), but that
    /// type is immutable once built. <see cref="IMultiMap{TKey, TValue}"/> is its mutable
    /// counterpart, and it implements <see cref="ILookup{TKey, TElement}"/> so it can be handed to
    /// any existing LINQ code unchanged.
    /// </remarks>
    /// <typeparam name="TKey">Type of the keys.</typeparam>
    /// <typeparam name="TValue">Type of the values.</typeparam>
    public interface IMultiMap<TKey, TValue> : ILookup<TKey, TValue>
    {
        /// <summary>
        /// <c>true</c> when the same value may be stored more than once under one key.
        /// </summary>
        bool AllowsDuplicateValues { get; }

        /// <summary>
        /// Total number of key/value pairs stored.
        /// </summary>
        /// <remarks>
        /// Distinct from <see cref="ILookup{TKey, TElement}.Count"/>, which the interface defines
        /// as the number of <em>keys</em>. A map holding <c>a =&gt; [1, 2]</c> has a
        /// <c>Count</c> of 1 and a <see cref="ValueCount"/> of 2.
        /// </remarks>
        int ValueCount { get; }

        /// <summary>
        /// Snapshot of every key currently in the map.
        /// </summary>
        IReadOnlyCollection<TKey> Keys { get; }

        /// <summary>
        /// Snapshot of every value currently in the map, including repeats across keys.
        /// </summary>
        IReadOnlyCollection<TValue> Values { get; }

        /// <summary>
        /// Adds <paramref name="value"/> under <paramref name="key"/>, creating the key if needed.
        /// </summary>
        /// <returns>
        /// <c>false</c> when <see cref="AllowsDuplicateValues"/> is <c>false</c> and the pair is
        /// already present; otherwise <c>true</c>.
        /// </returns>
        bool Add(TKey key, TValue value);

        /// <summary>
        /// Adds every element of <paramref name="values"/> under <paramref name="key"/>.
        /// </summary>
        /// <returns>How many were actually added.</returns>
        int AddRange(TKey key, IEnumerable<TValue> values);

        /// <summary>
        /// Removes a single occurrence of <paramref name="value"/> from <paramref name="key"/>.
        /// The key itself is dropped once its last value is gone.
        /// </summary>
        /// <returns><c>true</c> when a pair was removed.</returns>
        bool Remove(TKey key, TValue value);

        /// <summary>
        /// Removes <paramref name="key"/> and every value stored under it.
        /// </summary>
        /// <returns>How many values were removed.</returns>
        int RemoveAll(TKey key);

        /// <summary>
        /// Removes every key and value.
        /// </summary>
        void Clear();

        /// <summary>
        /// Whether <paramref name="key"/> holds at least one value.
        /// </summary>
        bool ContainsKey(TKey key);

        /// <summary>
        /// Whether the exact <paramref name="key"/>/<paramref name="value"/> pair is present.
        /// </summary>
        bool ContainsPair(TKey key, TValue value);

        /// <summary>
        /// Whether <paramref name="value"/> is stored under any key.
        /// </summary>
        bool ContainsValue(TValue value);

        /// <summary>
        /// Retrieves the values stored under <paramref name="key"/> without allocating on a miss.
        /// </summary>
        /// <param name="key">Key to look up.</param>
        /// <param name="values">A snapshot of the values, or <c>null</c> when the key is absent.</param>
        bool TryGetValues(TKey key, out IReadOnlyList<TValue> values);

        /// <summary>
        /// Snapshot of every key/value pair, one entry per pair.
        /// </summary>
        IEnumerable<KeyValuePair<TKey, TValue>> Pairs();

        /// <summary>
        /// Copies the contents into a plain dictionary of lists.
        /// </summary>
        Dictionary<TKey, List<TValue>> ToDictionary();
    }
}
