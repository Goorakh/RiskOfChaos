using R2API;
using RiskOfChaos.Content;
using RiskOfChaos.ModificationController.Projectile;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System.Collections;
using UnityEngine;

namespace RiskOfChaos.Patches.AttackHooks
{
    static class AttackMultiSpawnHook
    {
        public static bool TryMultiSpawn(AttackHookManager.FireAttackDelegate spawnFunc, in AttackInfo attackInfo)
        {
            if (spawnFunc == null)
                return false;

            if (attackInfo.ProcChainMask.HasAnyProc())
                return false;

            ProjectileModificationManager projectileModificationManager = ProjectileModificationManager.Instance;
            if (!projectileModificationManager)
                return false;

            int additionalBulletSpawnCount = projectileModificationManager.AdditionalSpawnCount;
            if (additionalBulletSpawnCount <= 0)
                return false;

            AttackInfo multiSpawnAttackInfo = attackInfo;
            multiSpawnAttackInfo.ProcChainMask.AddModdedProc(CustomProcTypes.Repeated);

            static IEnumerator spawnExtraAttacks(AttackHookManager.FireAttackDelegate spawnFunc, int spawnCount, AttackInfo attackInfo)
            {
                for (int i = 0; i < spawnCount; i++)
                {
                    yield return new WaitForSeconds(0.15f);

                    spawnFunc(attackInfo);
                }
            }

            MonoBehaviour coroutineHost = Stage.instance;
            if (!coroutineHost)
                coroutineHost = ProjectileModificationManager.Instance;

            coroutineHost.StartCoroutine(spawnExtraAttacks(spawnFunc, additionalBulletSpawnCount, multiSpawnAttackInfo));
            return true;
        }
    }
}
