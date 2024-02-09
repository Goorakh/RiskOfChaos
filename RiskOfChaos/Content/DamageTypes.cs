using R2API;
using RiskOfChaos.Components;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.Content
{
    public static class DamageTypes
    {
        /// <summary>
        /// Makes an instance of damage NonLethal for all players hit, except for if a player hit themselves
        /// </summary>
        public static DamageAPI.ModdedDamageType NonLethalToNonAttackerPlayers { get; private set; }

        /// <summary>
        /// Bypass armor if hitting yourself
        /// </summary>
        public static DamageAPI.ModdedDamageType BypassArmorSelf { get; private set; }

        /// <summary>
        /// Bypass block if hitting yourself
        /// </summary>
        public static DamageAPI.ModdedDamageType BypassBlockSelf { get; private set; }

        /// <summary>
        /// Bypass OSP if hitting yourself
        /// </summary>
        public static DamageAPI.ModdedDamageType BypassOSPSelf { get; private set; }

        [SystemInitializer]
        static void Init()
        {
            NonLethalToNonAttackerPlayers = DamageAPI.ReserveDamageType();
            BypassArmorSelf = DamageAPI.ReserveDamageType();
            BypassBlockSelf = DamageAPI.ReserveDamageType();
            BypassOSPSelf = DamageAPI.ReserveDamageType();

            On.RoR2.HealthComponent.TakeDamage += HealthComponent_TakeDamage;
        }

        static void HealthComponent_TakeDamage(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo)
        {
            GameObject attacker;
            if (damageInfo.inflictor &&
                damageInfo.inflictor.TryGetComponent(out GenericOwnership ownership) &&
                ownership.ownerObject)
            {
                attacker = ownership.ownerObject;
            }
            else
            {
                attacker = damageInfo.attacker;
            }

            if (attacker == self.gameObject)
            {
                if (damageInfo.HasModdedDamageType(BypassArmorSelf))
                    damageInfo.damageType |= DamageType.BypassArmor;

                if (damageInfo.HasModdedDamageType(BypassBlockSelf))
                    damageInfo.damageType |= DamageType.BypassBlock;

                if (damageInfo.HasModdedDamageType(BypassOSPSelf))
                    damageInfo.damageType |= DamageType.BypassOneShotProtection;
            }
            else
            {
                if (damageInfo.HasModdedDamageType(NonLethalToNonAttackerPlayers))
                {
                    CharacterBody body = self.body;
                    if (body && body.isPlayerControlled)
                    {
                        damageInfo.damageType |= DamageType.NonLethal;
                    }
                }
            }

            orig(self, damageInfo);
        }
    }
}
