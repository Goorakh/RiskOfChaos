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

        public static Quaternion RandomDeviation(float minDeviation, float maxDeviation, Xoroshiro128Plus rng)
        {
            float deviation;
            if (minDeviation >= maxDeviation)
            {
                deviation = Mathf.Min(minDeviation, maxDeviation);
            }
            else
            {
                deviation = rng.RangeFloat(minDeviation, maxDeviation);
            }

            Vector3 direction = Quaternion.AngleAxis(rng.RangeFloat(0f, 360f), Vector3.forward)
                                * (Quaternion.AngleAxis(deviation, Vector3.right) * Vector3.forward);

            return Util.QuaternionSafeLookRotation(direction);
        }

        public static Quaternion RandomDeviation(float maxDeviation, Xoroshiro128Plus rng)
        {
            return RandomDeviation(0, maxDeviation, rng);
        }
    }
}
