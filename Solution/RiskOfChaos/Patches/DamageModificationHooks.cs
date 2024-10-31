using MonoMod.Utils;
using RoR2;
using System;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Patches
{
    static class DamageModificationHooks
    {
        public delegate void ModifyDamageInfoDelegate(DamageInfo damageInfo);
        public static event ModifyDamageInfoDelegate ModifyDamageInfo;

        static readonly WeakReference _lastModifiedDamageInfoReference = new WeakReference(null);

        [SystemInitializer]
        static void Init()
        {
            On.RoR2.GlobalEventManager.OnHitAll += GlobalEventManager_OnHitAll;

            On.RoR2.GlobalEventManager.OnHitEnemy += GlobalEventManager_OnHitEnemy;

            On.RoR2.HealthComponent.TakeDamage += HealthComponent_TakeDamage;
        }

        static void tryModifyDamageInfo(DamageInfo damageInfo)
        {
            if (!NetworkServer.active || damageInfo == null || ModifyDamageInfo == null)
                return;

            if (_lastModifiedDamageInfoReference.SafeGetIsAlive() && ReferenceEquals(_lastModifiedDamageInfoReference.SafeGetTarget(), damageInfo))
                return;

            _lastModifiedDamageInfoReference.Target = damageInfo;

            ModifyDamageInfo(damageInfo);
        }

        static void GlobalEventManager_OnHitAll(On.RoR2.GlobalEventManager.orig_OnHitAll orig, GlobalEventManager self, DamageInfo damageInfo, GameObject hitObject)
        {
            tryModifyDamageInfo(damageInfo);
            orig(self, damageInfo, hitObject);
        }

        static void GlobalEventManager_OnHitEnemy(On.RoR2.GlobalEventManager.orig_OnHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
        {
            tryModifyDamageInfo(damageInfo);
            orig(self, damageInfo, victim);
        }

        static void HealthComponent_TakeDamage(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo)
        {
            tryModifyDamageInfo(damageInfo);
            orig(self, damageInfo);
        }
    }
}
