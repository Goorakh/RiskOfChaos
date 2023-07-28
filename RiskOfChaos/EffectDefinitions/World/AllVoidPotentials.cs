using HG;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RoR2;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosEffect("all_void_potentials")]
    [ChaosTimedEffect(TimedEffectType.UntilStageEnd, AllowDuplicates = false)]
    [EffectConfigBackwardsCompatibility("Effect: All Items Are Void Potentials (Lasts 1 stage)")]
    public sealed class AllVoidPotentials : TimedEffect
    {
        static readonly GameObject _optionPickupPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/OptionPickup/OptionPickup.prefab").WaitForCompletion();

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _optionPickupPrefab && (!RunArtifactManager.instance || !RunArtifactManager.instance.IsArtifactEnabled(RoR2Content.Artifacts.commandArtifactDef));
        }

        public override void OnStart()
        {
            PickupDropletController.onDropletHitGroundServer += onDropletHitGroundServer;
        }

        public override void OnEnd()
        {
            PickupDropletController.onDropletHitGroundServer -= onDropletHitGroundServer;
        }

        static void onDropletHitGroundServer(ref GenericPickupController.CreatePickupInfo createPickupInfo, ref bool shouldSpawn)
        {
            if (!shouldSpawn)
                return;

            PickupIndex pickupIndex = createPickupInfo.pickupIndex;
            if (!pickupIndex.isValid)
                return;

            PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndex);
            if (pickupDef == null)
                return;

            // Only create options if this pickup won't already have any
            if (createPickupInfo.pickerOptions != null)
                return;

            GameObject dropletDisplay = GameObject.Instantiate(_optionPickupPrefab, createPickupInfo.position, createPickupInfo.rotation);

            if (dropletDisplay.TryGetComponent(out PickupIndexNetworker pickupIndexNetworker))
            {
                pickupIndexNetworker.NetworkpickupIndex = pickupIndex;
            }

            if (dropletDisplay.TryGetComponent(out PickupPickerController pickupPickerController))
            {
                const int MAX_NUM_OPTIONS = 3;

                PickupIndex[] availablePickups = PickupTransmutationManager.GetAvailableGroupFromPickupIndex(pickupIndex);
                if (availablePickups != null && availablePickups.Length > 0)
                {
                    // The method returns a reference to an array, but since we will be shuffling it, make a shallow copy of it so the order of the original is not changed
                    PickupIndex[] shuffledPickupIndices = (PickupIndex[])availablePickups.Clone();
                    Util.ShuffleArray(shuffledPickupIndices, RoR2Application.rng);

                    int pickupsLength = shuffledPickupIndices.Length;

                    int originalPickupIndex = Array.IndexOf(shuffledPickupIndices, pickupIndex);
                    if (originalPickupIndex != -1)
                    {
                        ArrayUtils.ArrayRemoveAtAndResize(ref shuffledPickupIndices, originalPickupIndex);
                    }

                    int numOptions = Mathf.Min(pickupsLength, MAX_NUM_OPTIONS);
                    PickupPickerController.Option[] options = new PickupPickerController.Option[numOptions];

                    // Guarantee the original item is always an option
                    options[0] = new PickupPickerController.Option
                    {
                        available = true,
                        pickupIndex = pickupIndex
                    };

                    for (int i = 1; i < numOptions; i++)
                    {
                        options[i] = new PickupPickerController.Option
                        {
                            available = true,
                            pickupIndex = shuffledPickupIndices[i]
                        };
                    }

                    pickupPickerController.SetOptionsServer(options);
                }
                else
                {
                    pickupPickerController.SetOptionsServer(new PickupPickerController.Option[]
                    {
                        new PickupPickerController.Option
                        {
                            available = true,
                            pickupIndex = pickupIndex
                        }
                    });
                }
            }

            NetworkServer.Spawn(dropletDisplay);

            shouldSpawn = false;
        }
    }
}
