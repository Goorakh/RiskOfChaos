using System.Collections.Generic;

namespace RiskOfChaos.Utilities.Extensions
{
    public static class ListExtensions
    {
        public static T GetAndRemoveAt<T>(this List<T> list, int index)
        {
            T item = list[index];
            list.RemoveAt(index);
            return item;
        }

        public static T GetAndRemoveRandom<T>(this List<T> list, Xoroshiro128Plus rng)
        {
            return list.GetAndRemoveAt(rng.RangeInt(0, list.Count));
        }

        public static T GetAndRemoveRandom<T>(this List<T> list)
        {
            return list.GetAndRemoveAt(UnityEngine.Random.Range(0, list.Count));
        }
    }
}
