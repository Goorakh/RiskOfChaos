using RiskOfChaos.ModifierController.Projectile;
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

            if (!ProjectileModificationManager.Instance || ProjectileModificationManager.Instance.NetworkedExtraSpawnCount <= 0)
                return;

            // Don't allow procs to repeat
            if (fireProjectileInfo.procChainMask.mask != 0b0)
                return;

            IEnumerator spawnExtraProjectiles()
            {
                Stage startingStage = Stage.instance;

                for (byte i = 0; i < ProjectileModificationManager.Instance.NetworkedExtraSpawnCount; i++)
                {
                    yield return new WaitForSeconds(0.2f);

                    if (!ProjectileModificationManager.Instance || startingStage != Stage.instance || !self)
                        break;

                    _patchDisabled = true;
                    try
                    {
                        self.FireProjectile(fireProjectileInfo);
                    }
                    finally
                    {
                        _patchDisabled = false;
                    }
                }
            }

            ProjectileModificationManager.Instance.StartCoroutine(spawnExtraProjectiles());
        }
    }
}
