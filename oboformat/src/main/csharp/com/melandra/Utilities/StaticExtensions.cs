using System;
using System.Collections.Generic;

namespace com.melandra.Utilities
{
    public static class StaticExtensions
    {
        public static void ForEach<T>(this IEnumerable<T> victims, Action<T> action)
        {
            foreach (T victim in victims)
                action(victim);
        }

        public static void AddRange<K, V>(this IDictionary<K, V> target, IDictionary<K, V> source)
        {
            foreach (KeyValuePair<K, V> pair in source)
                target.Add(pair);
        }
    }
}
