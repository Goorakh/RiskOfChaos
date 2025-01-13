using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace RiskOfChaos.Utilities.Extensions
{
    public static class RNGExtensions
    {
        public static T NextElementUniform<T>(this Xoroshiro128Plus rng, IReadOnlyList<T> list)
        {
            return list[rng.RangeInt(0, list.Count)];
        }

        public static Vector3 RandomEuler(this Xoroshiro128Plus rng)
        {
            return new Vector3(rng.RangeFloat(-180f, 180f), rng.RangeFloat(-180f, 180f), rng.RangeFloat(-180f, 180f));
        }

        public static Quaternion RandomRotation(this Xoroshiro128Plus rng)
        {
            return Quaternion.Euler(rng.RandomEuler());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Xoroshiro128Plus Branch(this Xoroshiro128Plus rng)
        {
            return new Xoroshiro128Plus(rng.nextUlong);
        }
    }
}
