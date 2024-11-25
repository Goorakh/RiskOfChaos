using RiskOfChaos.ModificationController.Projectile;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.Orbs;
using System.Collections;
using System.Collections.ObjectModel;
using UnityEngine;

namespace RiskOfChaos.Patches
{
    static class OrbMultiSpawnHook
    {
        static bool _isFiringRepeat;

        [SystemInitializer]
        static void Init()
        {
            On.RoR2.Orbs.OrbManager.AddOrb += OrbManager_AddOrb;
        }

        static bool shouldSpawnRepeatOrbs(Orb orb)
        {
            if (_isFiringRepeat)
                return false;

            if (orb == null || OrbUtils.IsTransferOrb(orb))
                return false;

            if (!ProjectileModificationManager.Instance || ProjectileModificationManager.Instance.AdditionalSpawnCount <= 0)
                return false;

            if (orb.TryGetProcChainMask(out ProcChainMask procChainMask) && procChainMask.HasAnyProc())
                return false;

            if (orb.TryGetBouncedObjects(out ReadOnlyCollection<HealthComponent> bouncedObjects) && bouncedObjects.Count > 0)
                return false;

            return true;
        }

        static void OrbManager_AddOrb(On.RoR2.Orbs.OrbManager.orig_AddOrb orig, OrbManager self, Orb orb)
        {
            if (shouldSpawnRepeatOrbs(orb))
            {
                static IEnumerator spawnExtraOrbs(Orb orbTemplate, int spawnCount)
                {
                    Stage startingStage = Stage.instance;

                    for (int i = 0; i < spawnCount; i++)
                    {
                        yield return new WaitForSeconds(0.2f);

                        if (startingStage != Stage.instance || !OrbManager.instance)
                            break;

                        // Orb target no longer exists
                        if (!orbTemplate.target)
                            break;

                        _isFiringRepeat = true;
                        try
                        {
                            OrbManager.instance.AddOrb(OrbUtils.Clone(orbTemplate));
                        }
                        finally
                        {
                            _isFiringRepeat = false;
                        }
                    }
                }

                ProjectileModificationManager.Instance.StartCoroutine(spawnExtraOrbs(OrbUtils.Clone(orb), ProjectileModificationManager.Instance.AdditionalSpawnCount));
            }

            orig(self, orb);
        }
    }
}
