using RiskOfChaos.ModificationController.Projectile;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.Orbs;
using System.Collections;
using UnityEngine;

namespace RiskOfChaos.Patches
{
    static class OrbMultiSpawnHook
    {
        static bool _isAddingRepeat;

        [SystemInitializer]
        static void Init()
        {
            On.RoR2.Orbs.OrbManager.AddOrb += OrbManager_AddOrb;
        }

        static void OrbManager_AddOrb(On.RoR2.Orbs.OrbManager.orig_AddOrb orig, OrbManager self, Orb orb)
        {
            bool shouldSpawnAdditional = !_isAddingRepeat
                                         && orb != null
                                         && !OrbUtils.IsTransferOrb(orb)
                                         && ProjectileModificationManager.Instance
                                         && ProjectileModificationManager.Instance.AdditionalSpawnCount > 0
                                         // Don't allow procs to repeat
                                         && (!orb.TryGetProcChainMask(out ProcChainMask orbProcChain) || orbProcChain.Equals(default));

            // Clone original orb to use as a template, otherwise changes to the original instance will affect all the repeat orbs
            Orb orbTemplate = shouldSpawnAdditional ? OrbUtils.Clone(orb) : null;

            orig(self, orb);

            if (shouldSpawnAdditional)
            {
                IEnumerator spawnExtraOrbs(Orb orbTemplate, int spawnCount)
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

                        _isAddingRepeat = true;
                        try
                        {
                            OrbManager.instance.AddOrb(OrbUtils.Clone(orbTemplate));
                        }
                        finally
                        {
                            _isAddingRepeat = false;
                        }
                    }
                }

                ProjectileModificationManager.Instance.StartCoroutine(spawnExtraOrbs(orbTemplate, ProjectileModificationManager.Instance.AdditionalSpawnCount));
            }
        }
    }
}
