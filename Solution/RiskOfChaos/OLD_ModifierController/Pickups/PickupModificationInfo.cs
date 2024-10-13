using RiskOfChaos.Utilities.Interpolation;

namespace RiskOfChaos.OLD_ModifierController.Pickups
{
    public struct PickupModificationInfo
    {
        public uint BounceCount = 0;
        public float SpawnCountMultiplier = 1f;

        public PickupModificationInfo()
        {
        }

        public static PickupModificationInfo Interpolate(in PickupModificationInfo a, in PickupModificationInfo b, float t, ValueInterpolationFunctionType interpolationType)
        {
            return new PickupModificationInfo
            {
                BounceCount = interpolationType.Interpolate(a.BounceCount, b.BounceCount, t),
                SpawnCountMultiplier = interpolationType.Interpolate(a.BounceCount, b.BounceCount, t)
            };
        }
    }
}
