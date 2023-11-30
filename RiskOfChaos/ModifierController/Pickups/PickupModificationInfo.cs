namespace RiskOfChaos.ModifierController.Pickups
{
    public struct PickupModificationInfo
    {
        public uint BounceCount = 0;

        public PickupModificationInfo()
        {
        }

        public static PickupModificationInfo Interpolate(in PickupModificationInfo a, in PickupModificationInfo b, float t, ValueInterpolationFunctionType interpolationType)
        {
            return new PickupModificationInfo
            {
                BounceCount = interpolationType.Interpolate(a.BounceCount, b.BounceCount, t)
            };
        }
    }
}
