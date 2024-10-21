using RiskOfChaos.ModificationController.Projectile;
using RoR2;
using RoR2.Projectile;
using System.Collections;
using UnityEngine;

namespace RiskOfChaos.Patches
{
    static class ProjectileMultiSpawnHook
    {
        static bool _patchDisabled = false;

        [SystemInitializer]
        static void Init()
        {
            On.RoR2.Projectile.ProjectileManager.FireProjectile_FireProjectileInfo += ProjectileManager_FireProjectile_FireProjectileInfo;
        }

        static void ProjectileManager_FireProjectile_FireProjectileInfo(On.RoR2.Projectile.ProjectileManager.orig_FireProjectile_FireProjectileInfo orig, ProjectileManager self, FireProjectileInfo fireProjectileInfo)
        {
            orig(self, fireProjectileInfo);

            if (_patchDisabled)
                return;

            ProjectileModificationManager projectileModificationManager = ProjectileModificationManager.Instance;
            if (!projectileModificationManager)
                return;

            int additionalProjectileSpawnCount = projectileModificationManager.AdditionalSpawnCount;
            if (additionalProjectileSpawnCount <= 0)
                return;

            // Don't allow procs to repeat
            if (!fireProjectileInfo.procChainMask.Equals(default))
                return;

            IEnumerator spawnExtraProjectiles(FireProjectileInfo fireProjectileInfo, int spawnCount)
            {
                Stage startingStage = Stage.instance;

                for (int i = 0; i < spawnCount; i++)
                {
                    yield return new WaitForSeconds(0.2f);

                    if (startingStage != Stage.instance || !ProjectileManager.instance)
                        break;

                    _patchDisabled = true;
                    try
                    {
                        ProjectileManager.instance.FireProjectile(fireProjectileInfo);
                    }
                    finally
                    {
                        _patchDisabled = false;
                    }
                }
            }

            projectileModificationManager.StartCoroutine(spawnExtraProjectiles(fireProjectileInfo, additionalProjectileSpawnCount));
        }
    }
}
