using RiskOfChaos.Utilities.Interpolation;

namespace RiskOfChaos.ModifierController.Projectile
{
    public struct ProjectileModificationData
    {
        public float SpeedMultiplier = 1f;

        public uint ProjectileBounceCount;
        public uint BulletBounceCount;
        public uint OrbBounceCount;

        public ProjectileModificationData()
        {
        }

        public static ProjectileModificationData Interpolate(in ProjectileModificationData a, in ProjectileModificationData b, float t, ValueInterpolationFunctionType interpolationType)
        {
            return new ProjectileModificationData
            {
                SpeedMultiplier = interpolationType.Interpolate(a.SpeedMultiplier, b.SpeedMultiplier, t),
                ProjectileBounceCount = interpolationType.Interpolate(a.ProjectileBounceCount, b.ProjectileBounceCount, t),
                BulletBounceCount = interpolationType.Interpolate(a.BulletBounceCount, b.BulletBounceCount, t),
                OrbBounceCount = interpolationType.Interpolate(a.OrbBounceCount, b.OrbBounceCount, t)
            };
        }
    }
}
