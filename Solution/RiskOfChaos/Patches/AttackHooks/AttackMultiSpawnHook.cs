using RiskOfChaos.ModificationController.Projectile;
using RoR2;
using System;
using System.Collections;
using UnityEngine;

namespace RiskOfChaos.Patches.AttackHooks
{
    static class AttackMultiSpawnHook
    {
        [SystemInitializer]
        static void Init()
        {
            //OverrideBulletTracerOriginExplicitPatch.UseExplicitOriginPosition += _ => (AttackHookManager.Context.Peek() & AttackHookType.Repeat) != 0;
        }

        public static bool TryMultiSpawn(Action spawnFunc, AttackHookMask activeAttackHooks)
        {
            if (spawnFunc == null)
                return false;

            ProjectileModificationManager projectileModificationManager = ProjectileModificationManager.Instance;
            if (!projectileModificationManager)
                return false;

            int additionalBulletSpawnCount = projectileModificationManager.AdditionalSpawnCount;
            if (additionalBulletSpawnCount <= 0)
                return false;

            static IEnumerator spawnExtraAttacks(Action spawnFunc, int spawnCount, AttackHookMask activeAttackHooks)
            {
                for (int i = 0; i < spawnCount; i++)
                {
                    yield return new WaitForSeconds(0.15f);

                    AttackHookManager.Context.Activate(activeAttackHooks | AttackHookMask.Repeat);
                    spawnFunc();
                }
            }

            MonoBehaviour coroutineHost = Stage.instance;
            if (!coroutineHost)
                coroutineHost = ProjectileModificationManager.Instance;

            coroutineHost.StartCoroutine(spawnExtraAttacks(spawnFunc, additionalBulletSpawnCount, activeAttackHooks));
            return true;
        }
    }
}
