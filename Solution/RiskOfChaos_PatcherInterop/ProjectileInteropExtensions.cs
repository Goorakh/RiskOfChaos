using RoR2;
using RoR2.Projectile;

namespace RiskOfChaos_PatcherInterop
{
    public static class ProjectileInteropExtensions
    {
        static float? decodeProcCoefficientOverride(float overridePlusOne)
        {
            float procCoefficientOverride = overridePlusOne - 1f;
            if (procCoefficientOverride < 0f)
                return null;

            return procCoefficientOverride;
        }

        static float encodeProcCoefficientOverride(float? overrideProcCoefficient)
        {
            if (!overrideProcCoefficient.HasValue)
                return 0f;

            return overrideProcCoefficient.Value + 1f;
        }

        public static float GetProcCoefficientOverridePlusOne(this in FireProjectileInfo fireProjectileInfo)
        {
            return fireProjectileInfo.roc_procCoefficientOverridePlusOne;
        }

        public static float? GetProcCoefficientOverride(this in FireProjectileInfo fireProjectileInfo)
        {
            return decodeProcCoefficientOverride(fireProjectileInfo.roc_procCoefficientOverridePlusOne);
        }

        public static void SetProcCoefficientOverridePlusOne(this ref FireProjectileInfo fireProjectileInfo, float value)
        {
            fireProjectileInfo.roc_procCoefficientOverridePlusOne = value;
        }

        public static void SetProcCoefficientOverride(this ref FireProjectileInfo fireProjectileInfo, float? value)
        {
            fireProjectileInfo.roc_procCoefficientOverridePlusOne = encodeProcCoefficientOverride(value);
        }

        public static float GetProcCoefficientOverridePlusOne(this ProjectileManager.PlayerFireProjectileMessage playerFireProjectileMessage)
        {
            return playerFireProjectileMessage.roc_procCoefficientOverridePlusOne;
        }

        public static float? GetProcCoefficientOverride(this ProjectileManager.PlayerFireProjectileMessage playerFireProjectileMessage)
        {
            return decodeProcCoefficientOverride(playerFireProjectileMessage.roc_procCoefficientOverridePlusOne);
        }

        public static void SetProcCoefficientOverridePlusOne(this ProjectileManager.PlayerFireProjectileMessage playerFireProjectileMessage, float value)
        {
            playerFireProjectileMessage.roc_procCoefficientOverridePlusOne = value;
        }

        public static void SetProcCoefficientOverride(this ProjectileManager.PlayerFireProjectileMessage playerFireProjectileMessage, float? value)
        {
            playerFireProjectileMessage.roc_procCoefficientOverridePlusOne = encodeProcCoefficientOverride(value);
        }

        public static ProcChainMask GetProcChainMask(this ProjectileManager.PlayerFireProjectileMessage playerFireProjectileMessage)
        {
            return playerFireProjectileMessage.roc_procChainMask;
        }

        public static void SetProcChainMask(this ProjectileManager.PlayerFireProjectileMessage playerFireProjectileMessage, ProcChainMask procChainMask)
        {
            playerFireProjectileMessage.roc_procChainMask = procChainMask;
        }
    }
}
