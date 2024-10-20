using RiskOfChaos.ModificationController.Pickups;
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
            On.RoR2.PickupDropletController.CreatePickupDroplet_CreatePickupInfo_Vector3_Vector3 += PickupDropletController_CreatePickupDroplet_SpawnAdditional;
        }

        static bool _patchDisabled;

        static void PickupDropletController_CreatePickupDroplet_SpawnAdditional(On.RoR2.PickupDropletController.orig_CreatePickupDroplet_CreatePickupInfo_Vector3_Vector3 orig,
                                                                                GenericPickupController.CreatePickupInfo pickupInfo,
                                                                                Vector3 position,
                                                                                Vector3 velocity)
        {
            orig(pickupInfo, position, velocity);

            if (!NetworkServer.active || _patchDisabled)
                return;

            PickupModificationManager pickupModificationManager = PickupModificationManager.Instance;
            if (!pickupModificationManager)
                return;

            int extraSpawnCount = pickupModificationManager.ExtraSpawnCount;
            if (extraSpawnCount <= 0)
                return;
            
            IEnumerator spawnAdditionalDroplets(GenericPickupController.CreatePickupInfo pickupInfo, Vector3 position, Vector3 velocity, int extraSpawnCount)
            {
                Stage stage = Stage.instance;

                for (int i = 0; i < extraSpawnCount; i++)
                {
                    yield return new WaitForSeconds(Mathf.Max(1f / 30f, 0.2f * Mathf.Pow(0.925f, i)));

                    // Stage switched (or destroyed) since first drop, quit spawning new pickups
                    if (stage != Stage.instance)
                        break;

                    _patchDisabled = true;
                    try
                    {
                        PickupDropletController.CreatePickupDroplet(pickupInfo, position, velocity);
                    }
                    finally
                    {
                        _patchDisabled = false;
                    }
                }
            }

            pickupModificationManager.StartCoroutine(spawnAdditionalDroplets(pickupInfo, position, velocity, extraSpawnCount));
        }
    }
}
