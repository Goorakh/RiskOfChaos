using System;
using System.Collections.Generic;

namespace RiskOfChaos.Utilities.Extensions
{
    public static class DictionaryExtensions
    {
        public static TValue GetOrAddNew<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key) where TValue : new()
        {
            if (dictionary is null)
                throw new ArgumentNullException(nameof(dictionary));

            if (dictionary.TryGetValue(key, out TValue value))
                return value;

            value = new TValue();
            dictionary.Add(key, value);
            return value;
        }
    }
}
