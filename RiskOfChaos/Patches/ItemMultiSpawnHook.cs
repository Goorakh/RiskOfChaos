using RiskOfChaos.ModifierController.Pickups;
using RoR2;
using System.Collections;
using UnityEngine;

namespace RiskOfChaos.Patches
{
    static class ItemMultiSpawnHook
    {
        [SystemInitializer]
        static void Init()
        {
            On.RoR2.PickupDropletController.CreatePickupDroplet_CreatePickupInfo_Vector3_Vector3 += PickupDropletController_CreatePickupDroplet;
        }

        static void PickupDropletController_CreatePickupDroplet(On.RoR2.PickupDropletController.orig_CreatePickupDroplet_CreatePickupInfo_Vector3_Vector3 orig,
                                                                GenericPickupController.CreatePickupInfo pickupInfo,
                                                                Vector3 position,
                                                                Vector3 velocity)
        {
            orig(pickupInfo, position, velocity);

            if (PickupModificationManager.Instance && PickupModificationManager.Instance.SpawnCountMultiplier > 1)
            {
                IEnumerator spawnAdditionalDroplets()
                {
                    Stage stage = Stage.instance;

                    float waitTime = 0.2f;

                    // NOTE: Do not cache upper bound, we want an up-to-date value every iteration
                    for (int i = 0; i < PickupModificationManager.Instance.SpawnCountMultiplier - 1; i++)
                    {
                        yield return new WaitForSeconds(waitTime);
                        waitTime = Mathf.Max(1f / 30f, waitTime * 0.925f);

                        // Stage switched (or destroyed) since first drop, quit spawning new pickups
                        if (!PickupModificationManager.Instance || stage != Stage.instance)
                            break;

                        orig(pickupInfo, position, velocity);
                    }
                }

                PickupModificationManager.Instance.StartCoroutine(spawnAdditionalDroplets());
            }
        }
    }
}
