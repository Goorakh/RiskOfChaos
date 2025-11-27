using RiskOfChaos.Utilities.DropTables;
using RiskOfChaos.Utilities.Extensions;
using RiskOfChaos.Utilities.Pool;
using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectUtils.World.AllChanceShrines
{
    public readonly struct ShrineReplacementData
    {
        static readonly ScriptableObjectPool<SequentialPickupDropTable> _sequentialDropTablesPool = new ScriptableObjectPool<SequentialPickupDropTable>();
        static readonly ScriptableObjectPool<CombinedSequentialPickupDropTable> _combinationDropTablesPool = new ScriptableObjectPool<CombinedSequentialPickupDropTable>();

        [SystemInitializer]
        static void Init()
        {
            _sequentialDropTablesPool.WarmUp(15);
            _combinationDropTablesPool.WarmUp(15);
        }

        public readonly GameObject OriginalObject;
        public readonly Transform OriginalObjectTransform;
        public readonly PickupDropTable DropTable;

        public readonly UniquePickup[] RolledPickups;
        public readonly bool UseForcedPickupDropTable;

        public readonly bool ShouldSpawnShrine;

        ShrineReplacementData(GameObject originalObject, PickupDropTable dropTable, bool shouldSpawnShrine, UniquePickup[] rolledPickups, bool useForcedPickupDropTable)
        {
            OriginalObject = originalObject;
            OriginalObjectTransform = OriginalObject.transform;
            DropTable = dropTable;
            ShouldSpawnShrine = shouldSpawnShrine;
            RolledPickups = rolledPickups;
            UseForcedPickupDropTable = useForcedPickupDropTable;
        }

        ShrineReplacementData(GameObject originalObject) : this(originalObject, default, default, [], default)
        {
        }

        PickupDropTable createDropTable(GameObject shrineObject)
        {
            SequentialPickupDropTable rolledPickupsSequence = _sequentialDropTablesPool.GetOrCreateNew();
            rolledPickupsSequence.canDropBeReplaced = false;
            rolledPickupsSequence.Pickups = RolledPickups;

            rolledPickupsSequence.FinalizeManualSetup();

            OnDestroyCallback.AddCallback(shrineObject, _ =>
            {
                _sequentialDropTablesPool.Return(rolledPickupsSequence);
            });

            if (UseForcedPickupDropTable)
                return rolledPickupsSequence;

            CombinedSequentialPickupDropTable combinedDropTable = _combinationDropTablesPool.GetOrCreateNew();
            combinedDropTable.canDropBeReplaced = false;
            combinedDropTable.Entries = [
                new CombinedSequentialPickupDropTable.DropTableEntry(rolledPickupsSequence, RolledPickups.Length),
                new CombinedSequentialPickupDropTable.DropTableEntry(DropTable, int.MaxValue)
            ];

            combinedDropTable.FinalizeManualSetup();

            OnDestroyCallback.AddCallback(shrineObject, _ =>
            {
                _combinationDropTablesPool.Return(combinedDropTable);
            });

            return combinedDropTable;
        }

        public void PerformReplacement()
        {
            if (!ShouldSpawnShrine)
            {
                NetworkServer.Destroy(OriginalObject);
                return;
            }

            DirectorPlacementRule placementRule = new DirectorPlacementRule
            {
                placementMode = DirectorPlacementRule.PlacementMode.Direct,
                position = OriginalObjectTransform.position
            };

            InteractableSpawnCard shrineSpawnCard = FixedPositionChanceShrine.SpawnCard;

            DirectorSpawnRequest spawnRequest = new DirectorSpawnRequest(shrineSpawnCard, placementRule, RoR2Application.rng);
            SpawnCard.SpawnResult spawnResult = shrineSpawnCard.DoSpawn(OriginalObjectTransform.position, OriginalObjectTransform.rotation, spawnRequest);

            if (spawnResult.success && spawnResult.spawnedInstance && spawnResult.spawnedInstance.TryGetComponent(out ShrineChanceBehavior shrineChanceBehavior))
            {
                PickupDropTable shrineDropTable = createDropTable(spawnResult.spawnedInstance);
                shrineChanceBehavior.dropTable = shrineDropTable;
                shrineChanceBehavior.chanceDollDropTable = shrineDropTable;

                if (shrineChanceBehavior.TryGetComponent(out PurchaseInteraction shrinePurchaseInteraction))
                {
                    if (OriginalObject.TryGetComponent(out PurchaseInteraction purchaseInteraction))
                    {
                        shrinePurchaseInteraction.Networkcost = purchaseInteraction.cost;
                        shrinePurchaseInteraction.costType = purchaseInteraction.costType;
                    }
                    else
                    {
                        // If the original doesn't have a purchase interaction, default to no cost
                        shrinePurchaseInteraction.Networkcost = 0;
                        shrinePurchaseInteraction.costType = CostTypeIndex.None;
                    }
                }

                NetworkServer.Destroy(OriginalObject);
            }
        }

        public override string ToString()
        {
            return OriginalObject ? OriginalObject.ToString() : "null";
        }

        public static IEnumerable<ShrineReplacementData> GetReplacementDatasFor(PurchaseInteraction purchaseInteraction)
        {
            return GetReplacementDatasFor(purchaseInteraction.gameObject);
        }

        public static IEnumerable<ShrineReplacementData> GetReplacementDatasFor(GameObject interactableObject)
        {
            if (interactableObject.TryGetComponent(out EntityStateMachine esm))
            {
                if (esm.state is EntityStates.Barrel.Opened || (interactableObject.GetComponent<PickupDistributorBehavior>() && esm.state is EntityStates.Idle))
                {
                    yield break;
                }
            }

            PurchaseInteraction purchaseInteraction = interactableObject.GetComponent<PurchaseInteraction>();

            PickupDropTable dropTable;
            UniquePickup[] rolledPickups = [];
            bool useForcedPickupDropTable = false;

            if (interactableObject.TryGetComponent(out ChestBehavior chestBehavior))
            {
                dropTable = chestBehavior.dropTable;

                if (chestBehavior.currentPickup.isValid)
                {
                    rolledPickups = [ chestBehavior.currentPickup];
                }
            }
            else if (interactableObject.TryGetComponent(out RouletteChestController rouletteChestController))
            {
                dropTable = rouletteChestController.dropTable;

                RouletteChestController.Entry[] entries = rouletteChestController.entries;
                if (entries != null && entries.Length > 0)
                {
                    rolledPickups = Array.ConvertAll(entries, e => e.pickup);
                }
                else if (dropTable)
                {
                    Xoroshiro128Plus lootRNG = new Xoroshiro128Plus(rouletteChestController.rng);

                    rolledPickups = new UniquePickup[rouletteChestController.maxEntries];

                    UniquePickup lastPickup = UniquePickup.none;
                    for (int i = 0; i < rolledPickups.Length; i++)
                    {
                        UniquePickup pickupIndex = dropTable.GeneratePickup(lootRNG);
                        if (pickupIndex == lastPickup)
                            pickupIndex = dropTable.GeneratePickup(lootRNG);

                        rolledPickups[i] = pickupIndex;
                        lastPickup = pickupIndex;
                    }
                }
            }
            else if (interactableObject.TryGetComponent(out ShopTerminalBehavior shopTerminalBehavior))
            {
                if (!shopTerminalBehavior.pickup.isValid)
                {
                    yield break;
                }

                dropTable = shopTerminalBehavior.dropTable;

                UniquePickup pickup = shopTerminalBehavior.CurrentPickup();
                if (pickup.isValid)
                {
                    rolledPickups = [ pickup ];
                }

                if (shopTerminalBehavior.serverMultiShopController)
                {
                    yield return new ShrineReplacementData(shopTerminalBehavior.serverMultiShopController.gameObject);
                }
                else
                {
                    // no multishop controller -> assume printer
                    useForcedPickupDropTable = true;
                }
            }
            else if (interactableObject.TryGetComponent(out OptionChestBehavior optionChestBehavior))
            {
                dropTable = optionChestBehavior.dropTable;

                if (optionChestBehavior.generatedPickups != null)
                {
                    rolledPickups = [.. optionChestBehavior.generatedPickups];
                }
            }
            else if (interactableObject.TryGetComponent(out PickupDistributorBehavior pickupDistributorBehavior))
            {
                dropTable = pickupDistributorBehavior.dropTable;

                rolledPickups = [ pickupDistributorBehavior.pickup ];

                useForcedPickupDropTable = true;
            }
            else
            {
                yield break;
            }

            if (!dropTable)
            {
                yield break;
            }

            yield return new ShrineReplacementData(interactableObject, dropTable, true, rolledPickups, useForcedPickupDropTable);
        }
    }
}
