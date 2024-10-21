using RiskOfChaos.ModificationController.Projectile;
using RoR2;
using System.Collections;
using UnityEngine;

namespace RiskOfChaos.Patches
{
    static class BulletMultiSpawnHook
    {
        static bool _isFiringRepeat = false;

        [SystemInitializer]
        static void Init()
        {
            On.RoR2.BulletAttack.FireSingle += BulletAttack_FireSingle;

            OverrideBulletTracerOriginExplicitPatch.UseExplicitOriginPosition += _ => _isFiringRepeat;
        }

        static void BulletAttack_FireSingle(On.RoR2.BulletAttack.orig_FireSingle orig, BulletAttack self, Vector3 normal, int muzzleIndex)
        {
            orig(self, normal, muzzleIndex);

            if (_isFiringRepeat)
                return;

            ProjectileModificationManager projectileModificationManager = ProjectileModificationManager.Instance;
            if (!projectileModificationManager)
                return;

            int additionalBulletSpawnCount = projectileModificationManager.AdditionalSpawnCount;
            if (additionalBulletSpawnCount <= 0)
                return;

            // Don't allow procs to repeat
            if (!self.procChainMask.Equals(default))
                return;

            static IEnumerator spawnExtraBullets(BulletAttack bulletAttack, Vector3 direction, int muzzleIndex, int spawnCount)
            {
                Stage startingStage = Stage.instance;

                for (int i = 0; i < spawnCount; i++)
                {
                    yield return new WaitForSeconds(0.15f);

                    if (startingStage != Stage.instance)
                        break;

                    _isFiringRepeat = true;
                    try
                    {
                        bulletAttack.FireSingle(direction, muzzleIndex);
                    }
                    finally
                    {
                        _isFiringRepeat = false;
                    }
                }
            }

            projectileModificationManager.StartCoroutine(spawnExtraBullets(self, normal, muzzleIndex, additionalBulletSpawnCount));
        }
    }
}
