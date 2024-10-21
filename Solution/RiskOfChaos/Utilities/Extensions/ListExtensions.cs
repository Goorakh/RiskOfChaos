using System.Collections.Generic;

namespace RiskOfChaos.Utilities.Extensions
{
    public static class ListExtensions
    {
        public static T GetAndRemoveAt<T>(this IList<T> list, int index)
        {
            T item = list[index];
            list.RemoveAt(index);
            return item;
        }

        public static T GetAndRemoveRandom<T>(this IList<T> list, Xoroshiro128Plus rng)
        {
            return list.GetAndRemoveAt(rng.RangeInt(0, list.Count));
        }

        public static T GetAndRemoveRandom<T>(this IList<T> list)
        {
            return list.GetAndRemoveAt(UnityEngine.Random.Range(0, list.Count));
        }

        public static void EnsureCapacity<T>(this List<T> list, int capacity)
        {
            if (list.Capacity < capacity)
                list.Capacity = capacity;
        }

        public static void EnsureAdditionalCapacity<T>(this List<T> list, int additionalCapacity)
        {
            list.EnsureCapacity(list.Count + additionalCapacity);
        }
    }
}
