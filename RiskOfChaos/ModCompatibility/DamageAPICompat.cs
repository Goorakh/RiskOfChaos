using BepInEx.Bootstrap;
using R2API;
using RoR2;
using System;

namespace RiskOfChaos.ModCompatibility
{
    static class DamageAPICompat
    {
        public static bool Active => Chainloader.PluginInfos.ContainsKey(R2API.DamageAPI.PluginGUID);

        public static void CopyModdedDamageTypes(DamageInfo src, DamageInfo dest)
        {
            for (int i = 0; i < DamageAPI.ModdedDamageTypeCount; i++)
            {
                DamageAPI.ModdedDamageType damageType = (DamageAPI.ModdedDamageType)i;
                if (src.HasModdedDamageType(damageType))
                {
                    dest.AddModdedDamageType(damageType);
                }
            }
        }
    }
}
