using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CoreLite
{
    public static class CoreDictionExtensions
    {
        public static void AddIfNotExists<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (!dictionary.ContainsKey(key))
                dictionary.Add(key, value);
        }
     }
}

