using System.Collections.Generic;

namespace MultiMaps
{
    /// <summary>
    /// A thread-safe, mutable dictionary holding multiple values per key.
    /// </summary>
    /// <remarks>
    /// Identical in behaviour to <see cref="MultiMap{TKey, TValue}"/>; it exists so the type can be
    /// found under the name Microsoft used in the unreleased
    /// <c>Microsoft.Experimental.Collections</c> package. Pick whichever name reads better in your
    /// codebase — the two are interchangeable, and this one derives from the other.
    /// </remarks>
    /// <typeparam name="TKey">Type of the keys.</typeparam>
    /// <typeparam name="TValue">Type of the values.</typeparam>
    public class MultiValueDictionary<TKey, TValue> : MultiMap<TKey, TValue>
    {
        /// <summary>
        /// Creates an empty dictionary that allows duplicate values under a key.
        /// </summary>
        public MultiValueDictionary()
        {
        }

        /// <summary>
        /// Creates an empty dictionary.
        /// </summary>
        /// <param name="allowDuplicateValues">
        /// When <c>false</c>, adding a value already present under that key is rejected.
        /// </param>
        public MultiValueDictionary(bool allowDuplicateValues)
            : base(allowDuplicateValues)
        {
        }

        /// <summary>
        /// Creates an empty dictionary with explicit comparers.
        /// </summary>
        /// <param name="allowDuplicateValues">
        /// When <c>false</c>, adding a value already present under that key is rejected.
        /// </param>
        /// <param name="keyComparer">Comparer for keys, or <c>null</c> for the default.</param>
        /// <param name="valueComparer">Comparer for values, or <c>null</c> for the default.</param>
        public MultiValueDictionary(
            bool allowDuplicateValues,
            IEqualityComparer<TKey> keyComparer,
            IEqualityComparer<TValue> valueComparer)
            : base(allowDuplicateValues, keyComparer, valueComparer)
        {
        }

        /// <summary>
        /// Creates a dictionary populated from existing pairs.
        /// </summary>
        /// <param name="pairs">Pairs to copy in.</param>
        /// <param name="allowDuplicateValues">
        /// When <c>false</c>, repeated pairs in <paramref name="pairs"/> are collapsed to one.
        /// </param>
        public MultiValueDictionary(
            IEnumerable<KeyValuePair<TKey, TValue>> pairs,
            bool allowDuplicateValues = true)
            : base(pairs, allowDuplicateValues)
        {
        }
    }
}
