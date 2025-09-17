using RoR2.Projectile;

namespace RiskOfChaos.PatcherInterop
{
    internal static class ProjectileInteropExtensions
    {
        public static float GetProcCoefficientOverridePlusOne(this in FireProjectileInfo fireProjectileInfo)
        {
            return fireProjectileInfo.roc_procCoefficientOverridePlusOne;
        }

        public static float? GetProcCoefficientOverride(this in FireProjectileInfo fireProjectileInfo)
        {
            return InteropUtils.DecodePackedOverrideValue(fireProjectileInfo.roc_procCoefficientOverridePlusOne);
        }

        public static void SetProcCoefficientOverridePlusOne(this ref FireProjectileInfo fireProjectileInfo, float value)
        {
            fireProjectileInfo.roc_procCoefficientOverridePlusOne = value;
        }

        public static void SetProcCoefficientOverride(this ref FireProjectileInfo fireProjectileInfo, float? value)
        {
            fireProjectileInfo.roc_procCoefficientOverridePlusOne = InteropUtils.EncodePackedOverrideValue(value);
        }
    }
}
