using RiskOfChaos.ModifierController.Pickups;
using RoR2;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Patches
{
    static class ItemMultiSpawnHook
    {
        [SystemInitializer]
        static void Init()
        {
            On.RoR2.PickupDropletController.CreatePickupDroplet_CreatePickupInfo_Vector3 += PickupDropletController_CreatePickupDroplet;
        }

        static bool _patchDisabled;

        static void PickupDropletController_CreatePickupDroplet(On.RoR2.PickupDropletController.orig_CreatePickupDroplet_CreatePickupInfo_Vector3 orig,
                                                                GenericPickupController.CreatePickupInfo pickupInfo,
                                                                Vector3 velocity)
        {
            orig(pickupInfo, velocity);
            
            if (!NetworkServer.active || _patchDisabled)
                return;

            if (PickupModificationManager.Instance && PickupModificationManager.Instance.SpawnCountMultiplier > 1)
            {
                IEnumerator spawnAdditionalDroplets()
                {
                    Stage stage = Stage.instance;

                    // NOTE: Do not cache upper bound, we want an up-to-date value every iteration
                    for (int i = 0; i < PickupModificationManager.Instance.SpawnCountMultiplier - 1; i++)
                    {
                        yield return new WaitForSeconds(Mathf.Max(1f / 30f, 0.2f * Mathf.Pow(0.925f, i)));

                        // Stage switched (or destroyed) since first drop, quit spawning new pickups
                        if (!PickupModificationManager.Instance || stage != Stage.instance)
                            break;

                        _patchDisabled = true;
                        try
                        {
                            PickupDropletController.CreatePickupDroplet(pickupInfo, velocity);
                        }
                        finally
                        {
                            _patchDisabled = false;
                        }
                    }
                }

                PickupModificationManager.Instance.StartCoroutine(spawnAdditionalDroplets());
            }
        }
    }
}
