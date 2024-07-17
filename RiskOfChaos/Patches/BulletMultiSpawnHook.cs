using MonoMod.Cil;
using RiskOfChaos.ModifierController.Projectile;
using RoR2;
using RoR2.Projectile;
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

            if (!ProjectileModificationManager.Instance || ProjectileModificationManager.Instance.ExtraSpawnCount <= 0)
                return;

            // Don't allow procs to repeat
            if (!self.procChainMask.Equals(default))
                return;

            IEnumerator spawnExtraBullets()
            {
                Stage startingStage = Stage.instance;

                for (byte i = 0; i < ProjectileModificationManager.Instance.ExtraSpawnCount; i++)
                {
                    yield return new WaitForSeconds(0.15f);

                    if (!ProjectileModificationManager.Instance || startingStage != Stage.instance)
                        break;

                    _isFiringRepeat = true;
                    try
                    {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                        self.FireSingle(normal, muzzleIndex);
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
                    }
                    finally
                    {
                        _isFiringRepeat = false;
                    }
                }
            }

            ProjectileModificationManager.Instance.StartCoroutine(spawnExtraBullets());
        }
    }
}
