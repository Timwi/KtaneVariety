using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Variety
{
    static class Ut
    {
        public static T[] NewArray<T>(params T[] array) { return array; }
        public static T[] NewArray<T>(int length, Func<int, T> initializer)
        {
            var array = new T[length];
            for (var i = 0; i < length; i++)
                array[i] = initializer(i);
            return array;
        }

        /// <summary>
        ///     Enumerates all consecutive pairs of the elements.</summary>
        /// <param name="source">
        ///     The input enumerable.</param>
        /// <param name="closed">
        ///     If true, an additional pair containing the last and first element is included. For example, if the source
        ///     collection contains { 1, 2, 3, 4 } then the enumeration contains { (1, 2), (2, 3), (3, 4) } if <paramref
        ///     name="closed"/> is <c>false</c>, and { (1, 2), (2, 3), (3, 4), (4, 1) } if <paramref name="closed"/> is
        ///     <c>true</c>.</param>
        /// <param name="selector">
        ///     The selector function to run each consecutive pair through.</param>
        public static IEnumerable<TResult> SelectConsecutivePairs<T, TResult>(this IEnumerable<T> source, bool closed, Func<T, T, TResult> selector)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (selector == null)
                throw new ArgumentNullException("selector");
            return selectConsecutivePairsIterator(source, closed, selector);
        }
        private static IEnumerable<TResult> selectConsecutivePairsIterator<T, TResult>(IEnumerable<T> source, bool closed, Func<T, T, TResult> selector)
        {
            using (var enumer = source.GetEnumerator())
            {
                bool any = enumer.MoveNext();
                if (!any)
                    yield break;
                T first = enumer.Current;
                T last = enumer.Current;
                while (enumer.MoveNext())
                {
                    yield return selector(last, enumer.Current);
                    last = enumer.Current;
                }
                if (closed)
                    yield return selector(last, first);
            }
        }

        /// <summary>
        ///     Enumerates the items of this collection, skipping the last <paramref name="count"/> items. Note that the
        ///     memory usage of this method is proportional to <paramref name="count"/>, but the source collection is only
        ///     enumerated once, and in a lazy fashion. Also, enumerating the first item will take longer than enumerating
        ///     subsequent items.</summary>
        /// <param name="source">
        ///     Source collection.</param>
        /// <param name="count">
        ///     Number of items to skip from the end of the collection.</param>
        /// <param name="throwIfNotEnough">
        ///     If <c>true</c>, the enumerator throws at the end of the enumeration if the source collection contained fewer
        ///     than <paramref name="count"/> elements.</param>
        public static IEnumerable<T> SkipLast<T>(this IEnumerable<T> source, int count, bool throwIfNotEnough = false)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            if (count < 0)
                throw new ArgumentOutOfRangeException("count", "count cannot be negative.");
            if (count == 0)
                return source;

            var collection = source as ICollection<T>;
            if (collection != null)
            {
                if (throwIfNotEnough && collection.Count < count)
                    throw new InvalidOperationException("The collection does not contain enough elements.");
                return collection.Take(Math.Max(0, collection.Count - count));
            }

            return skipLastIterator(source, count, throwIfNotEnough);
        }
        private static IEnumerable<T> skipLastIterator<T>(IEnumerable<T> source, int count, bool throwIfNotEnough)
        {
            var queue = new T[count];
            int headtail = 0; // tail while we're still collecting, both head & tail afterwards because the queue becomes completely full
            int collected = 0;

            foreach (var item in source)
            {
                if (collected < count)
                {
                    queue[headtail] = item;
                    headtail++;
                    collected++;
                }
                else
                {
                    if (headtail == count)
                        headtail = 0;
                    yield return queue[headtail];
                    queue[headtail] = item;
                    headtail++;
                }
            }

            if (throwIfNotEnough && collected < count)
                throw new InvalidOperationException("The collection does not contain enough elements.");
        }

        public static IEnumerable<TResult> SelectManyConsecutivePairs<T, TResult>(this IEnumerable<T> source, bool closed, Func<T, T, IEnumerable<TResult>> selector)
        {
            return source.SelectConsecutivePairs(closed, selector).SelectMany(x => x);
        }

        public static Type FindType(string fullName)
        {
            return AppDomain.CurrentDomain.GetAssemblies().SelectMany(GetSafeTypes).FirstOrDefault(t => fullName.Equals(t.FullName));
        }
        public static Type FindType(string fullName, string assemblyName)
        {
            return AppDomain.CurrentDomain.GetAssemblies().SelectMany(GetSafeTypes).FirstOrDefault(t => fullName.Equals(t.FullName) && t.Assembly.GetName().Name.Equals(assemblyName));
        }
        public static IEnumerable<Type> GetSafeTypes(this Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(x => x != null);
            }
            catch (Exception)
            {
                return new List<Type>();
            }
        }

        /// <summary>
        ///     Returns a random element from the specified collection.</summary>
        /// <typeparam name="T">
        ///     The type of the elements in the collection.</typeparam>
        /// <param name="src">
        ///     The collection to pick from.</param>
        /// <param name="rnd">
        ///     Optionally, a random number generator to use.</param>
        /// <returns>
        ///     The element randomly picked.</returns>
        /// <remarks>
        ///     This method enumerates the entire input sequence into an array.</remarks>
        public static T PickRandom<T>(this IEnumerable<T> src, Random rnd)
        {
            var list = (src as IList<T>) ?? src.ToArray();
            if (list.Count == 0)
                throw new InvalidOperationException("Cannot pick an element from an empty set.");
            return list[rnd.Next(0, list.Count)];
        }

        /// <summary>
        ///     Brings the elements of the given list into a random order.</summary>
        /// <typeparam name="T">
        ///     Type of elements in the list.</typeparam>
        /// <param name="list">
        ///     List to shuffle.</param>
        /// <returns>
        ///     The list operated on.</returns>
        public static T Shuffle<T>(this T list, Random rnd) where T : IList
        {
            if (list == null)
                throw new ArgumentNullException("list");
            for (int j = list.Count; j >= 1; j--)
            {
                int item = rnd.Next(0, j);
                if (item < j - 1)
                {
                    var t = list[item];
                    list[item] = list[j - 1];
                    list[j - 1] = t;
                }
            }
            return list;
        }
    }
}
