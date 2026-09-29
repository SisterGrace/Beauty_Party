using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sky
{
    public static class SkyDictionExtensions
    {
        public static void AddIfNotExists<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (!dictionary.ContainsKey(key))
                dictionary.Add(key, value);
        }
     }
}

