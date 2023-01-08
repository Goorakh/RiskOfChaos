using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskOfChaos.Utilities.Extensions
{
    public static class RNGExtensions
    {
        public static Vector3 RandomEuler(this Xoroshiro128Plus rng)
        {
            return new Vector3(rng.RangeFloat(-180f, 180f), rng.RangeFloat(-180f, 180f), rng.RangeFloat(-180f, 180f));
        }

        public static Quaternion RandomRotation(this Xoroshiro128Plus rng)
        {
            return Quaternion.Euler(rng.RandomEuler());
        }
    }
}
