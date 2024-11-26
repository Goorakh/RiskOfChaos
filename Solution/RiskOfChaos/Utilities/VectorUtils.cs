using UnityEngine;

namespace RiskOfChaos.Utilities
{
    public static class VectorUtils
    {
        public static Vector3 Spread(Vector3 direction, float maxAngle, Xoroshiro128Plus rng)
        {
            return Spread(direction, 0f, maxAngle, rng);
        }

        public static Vector3 Spread(Vector3 direction, float minAngle, float maxAngle, Xoroshiro128Plus rng)
        {
            return QuaternionUtils.Spread(direction, minAngle, maxAngle, rng) * direction;
        }
    }
}
