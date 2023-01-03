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

        // https://karthikkaranth.me/blog/generating-random-points-in-a-sphere/
        public static Vector3 RandomPointInUnitSphere(this Xoroshiro128Plus rng)
        {
            float u = rng.nextNormalizedFloat;
            float v = rng.nextNormalizedFloat;
            float theta = u * (2f * Mathf.PI);
            float phi = Mathf.Acos((2f * v) - 1f);
            float r = Mathf.Pow(rng.nextNormalizedFloat, 1 / 3f);
            float sinTheta = Mathf.Sin(theta);
            float cosTheta = Mathf.Cos(theta);
            float sinPhi = Mathf.Sin(phi);
            float cosPhi = Mathf.Cos(phi);
            float x = r * sinPhi * cosTheta;
            float y = r * sinPhi * sinTheta;
            float z = r * cosPhi;
            return new Vector3(x, y, z);
        }
    }
}
