using System;
using System.Collections.Generic;

namespace MultiMaps
{
    /// <summary>
    /// LINQ-style entry points for building a <see cref="MultiMap{TKey, TValue}"/>.
    /// </summary>
    /// <remarks>
    /// These mirror <c>Enumerable.ToLookup</c>, so an existing <c>ToLookup</c> call can be swapped
    /// for <c>ToMultiMap</c> when the result needs to stay mutable.
    /// </remarks>
    public static class MultiMapExtensions
    {
        /// <summary>
        /// Builds a mutable multi-map by grouping <paramref name="source"/> on
        /// <paramref name="keySelector"/>.
        /// </summary>
        /// <typeparam name="TSource">Element type of the source sequence.</typeparam>
        /// <typeparam name="TKey">Type of the keys.</typeparam>
        /// <param name="source">Sequence to read.</param>
        /// <param name="keySelector">Produces the key for each element.</param>
        /// <param name="allowDuplicateValues">
        /// When <c>false</c>, repeated values under one key are collapsed to a single entry.
        /// </param>
        public static MultiMap<TKey, TSource> ToMultiMap<TSource, TKey>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            bool allowDuplicateValues = true)
        {
            return source.ToMultiMap(keySelector, x => x, allowDuplicateValues);
        }

        /// <summary>
        /// Builds a mutable multi-map by grouping <paramref name="source"/> on
        /// <paramref name="keySelector"/> and projecting each element with
        /// <paramref name="valueSelector"/>.
        /// </summary>
        /// <typeparam name="TSource">Element type of the source sequence.</typeparam>
        /// <typeparam name="TKey">Type of the keys.</typeparam>
        /// <typeparam name="TValue">Type of the values.</typeparam>
        /// <param name="source">Sequence to read.</param>
        /// <param name="keySelector">Produces the key for each element.</param>
        /// <param name="valueSelector">Produces the stored value for each element.</param>
        /// <param name="allowDuplicateValues">
        /// When <c>false</c>, repeated values under one key are collapsed to a single entry.
        /// </param>
        public static MultiMap<TKey, TValue> ToMultiMap<TSource, TKey, TValue>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TValue> valueSelector,
            bool allowDuplicateValues = true)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));
            if (valueSelector == null) throw new ArgumentNullException(nameof(valueSelector));

            var map = new MultiMap<TKey, TValue>(allowDuplicateValues);
            foreach (var item in source)
            {
                map.Add(keySelector(item), valueSelector(item));
            }
            return map;
        }
    }
}
