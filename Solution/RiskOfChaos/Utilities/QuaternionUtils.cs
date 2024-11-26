using HG;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.Utilities
{
    public static class QuaternionUtils
    {
        public static Quaternion PointLocalDirectionAt(Vector3 localDirection, Vector3 targetDirection)
        {
            return Util.QuaternionSafeLookRotation(targetDirection) * Quaternion.FromToRotation(localDirection, Vector3.forward);
        }

        public static Quaternion Spread(Vector3 direction, float minAngle, float maxAngle, Xoroshiro128Plus rng)
        {
            direction = direction.normalized;

            float angle = rng.RangeFloat(minAngle, maxAngle);
            Vector3 axis = Vector3.Cross(direction, rng.PointOnUnitSphere()).normalized;

            return Quaternion.AngleAxis(angle, axis);
        }

        public static Quaternion Spread(Vector3 baseDirection, float maxAngle, Xoroshiro128Plus rng)
        {
            return Spread(baseDirection, 0f, maxAngle, rng);
        }
    }
}
