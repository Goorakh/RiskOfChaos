﻿using R2API;
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
        public static DamageAPI.ModdedDamageType NonLethalToPlayers { get; private set; }

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
            NonLethalToPlayers = DamageAPI.ReserveDamageType();
            BypassArmorSelf = DamageAPI.ReserveDamageType();
            BypassBlockSelf = DamageAPI.ReserveDamageType();
            BypassOSPSelf = DamageAPI.ReserveDamageType();

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
