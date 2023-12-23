using RiskOfChaos.Utilities.Interpolation;

namespace RiskOfChaos.ModifierController.PhysicsModification
{
    public struct PhysicsModificationInfo
    {
        public float SpeedMultiplier = 1f;

        public PhysicsModificationInfo()
        {
        }

        public static PhysicsModificationInfo Interpolate(in PhysicsModificationInfo a, in PhysicsModificationInfo b, float t, ValueInterpolationFunctionType interpolationType)
        {
            return new PhysicsModificationInfo
            {
                SpeedMultiplier = interpolationType.Interpolate(a.SpeedMultiplier, b.SpeedMultiplier, t)
            };
        }
    }
}
