using HG;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RiskOfChaos.Utilities
{
    public static class UnityObjectUtils
    {
        public static int RemoveAllDestroyed<T>(IList<T> list) where T : UnityEngine.Object
        {
            if (list is null)
                throw new ArgumentNullException(nameof(list));

            int numRemovedItems = 0;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (!list[i])
                {
                    list.RemoveAt(i);
                    numRemovedItems++;
                }
            }

            return numRemovedItems;
        }

        public static int RemoveAllDestroyed<TKey, TValue>(IDictionary<UnityObjectWrapperKey<TKey>, TValue> dictionary) where TKey : UnityEngine.Object
        {
            if (dictionary is null)
                throw new ArgumentNullException(nameof(dictionary));

            int numRemovedItems = 0;

            UnityObjectWrapperKey<TKey>[] keys = new UnityObjectWrapperKey<TKey>[dictionary.Count];
            dictionary.Keys.CopyTo(keys, 0);

            foreach (UnityObjectWrapperKey<TKey> keyWrapper in keys)
            {
                TKey key = keyWrapper;
                if (!key)
                {
                    if (dictionary.Remove(keyWrapper))
                    {
                        numRemovedItems++;
                    }
                }
            }

            return numRemovedItems;
        }
    }
}
