using R2API;
using RiskOfChaos.Patches;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.Content
{
    public static class DamageTypes
    {
        /// <summary>
        /// Makes an instance of damage NonLethal for all players hit
        /// </summary>
        public static readonly DamageAPI.ModdedDamageType NonLethalToPlayers = DamageAPI.ReserveDamageType();

        /// <summary>
        /// Bypass armor if hitting yourself
        /// </summary>
        public static readonly DamageAPI.ModdedDamageType BypassArmorSelf = DamageAPI.ReserveDamageType();

        /// <summary>
        /// Bypass block if hitting yourself
        /// </summary>
        public static readonly DamageAPI.ModdedDamageType BypassBlockSelf = DamageAPI.ReserveDamageType();

        /// <summary>
        /// Bypass OSP if hitting yourself
        /// </summary>
        public static readonly DamageAPI.ModdedDamageType BypassOSPSelf = DamageAPI.ReserveDamageType();

        [SystemInitializer]
        static void Init()
        {
            HealthComponentHooks.PreTakeDamage += HealthComponentHooks_PreTakeDamage;
        }

        static void HealthComponentHooks_PreTakeDamage(HealthComponent healthComponent, DamageInfo damageInfo)
        {
            GameObject attacker = damageInfo.attacker;
            if (damageInfo.inflictor && damageInfo.inflictor.TryGetComponent(out GenericOwnership ownership))
            {
                GameObject ownerObject = ownership.ownerObject;
                if (ownerObject)
                {
                    attacker = ownerObject;
                }
            }

            if (attacker == healthComponent.gameObject)
            {
                if (damageInfo.damageType.HasModdedDamageType(BypassArmorSelf))
                    damageInfo.damageType |= DamageType.BypassArmor;

                if (damageInfo.damageType.HasModdedDamageType(BypassBlockSelf))
                    damageInfo.damageType |= DamageType.BypassBlock;

                if (damageInfo.damageType.HasModdedDamageType(BypassOSPSelf))
                    damageInfo.damageType |= DamageType.BypassOneShotProtection;
            }

            if (damageInfo.damageType.HasModdedDamageType(NonLethalToPlayers))
            {
                CharacterBody body = healthComponent.body;
                if (body && body.isPlayerControlled)
                {
                    damageInfo.damageType |= DamageType.NonLethal;
                }
            }
        }
    }
}
