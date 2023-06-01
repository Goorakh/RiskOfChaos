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

        public static Quaternion RandomDeviation(float deviation, Xoroshiro128Plus rng)
        {
            return Quaternion.Euler(rng.RangeFloat(-deviation, deviation),
                                    rng.RangeFloat(-deviation, deviation),
                                    rng.RangeFloat(-deviation, deviation));
        }
    }
}
