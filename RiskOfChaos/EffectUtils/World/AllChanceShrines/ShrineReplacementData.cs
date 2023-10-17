using RiskOfChaos.Utilities;
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

        public readonly PickupIndex[] RolledPickups;
        public readonly bool UseForcedPickupDropTable;

        public readonly bool ShouldSpawnShrine;

        ShrineReplacementData(GameObject originalObject, PickupDropTable dropTable, bool shouldSpawnShrine, PickupIndex[] rolledPickups, bool useForcedPickupDropTable)
        {
            OriginalObject = originalObject;
            OriginalObjectTransform = OriginalObject.transform;
            DropTable = dropTable;
            ShouldSpawnShrine = shouldSpawnShrine;
            RolledPickups = rolledPickups;
            UseForcedPickupDropTable = useForcedPickupDropTable;
        }

        ShrineReplacementData(GameObject originalObject) : this(originalObject, default, default, default, default)
        {
        }

        PickupDropTable createDropTable(GameObject shrineObject)
        {
            if (!Configs.General.SeededEffectSelection.Value)
                return DropTable;

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
            combinedDropTable.Entries = new CombinedSequentialPickupDropTable.DropTableEntry[]
            {
                new CombinedSequentialPickupDropTable.DropTableEntry(rolledPickupsSequence, RolledPickups.Length),
                new CombinedSequentialPickupDropTable.DropTableEntry(DropTable, int.MaxValue)
            };

            combinedDropTable.FinalizeManualSetup();

            OnDestroyCallback.AddCallback(shrineObject, _ =>
            {
                _combinationDropTablesPool.Return(combinedDropTable);
            });

            return combinedDropTable;
        }

        public void PerformReplacement(Xoroshiro128Plus rng)
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

            DirectorSpawnRequest spawnRequest = new DirectorSpawnRequest(shrineSpawnCard, placementRule, rng);
            SpawnCard.SpawnResult spawnResult = shrineSpawnCard.DoSpawn(OriginalObjectTransform.position, OriginalObjectTransform.rotation, spawnRequest);

            if (spawnResult.success && spawnResult.spawnedInstance && spawnResult.spawnedInstance.TryGetComponent(out ShrineChanceBehavior shrineChanceBehavior))
            {
                shrineChanceBehavior.dropTable = createDropTable(spawnResult.spawnedInstance);

                if (shrineChanceBehavior.TryGetComponent(out PurchaseInteraction shrinePurchaseInteraction))
                {
                    if (OriginalObject.TryGetComponent(out PurchaseInteraction purchaseInteraction))
                    {
                        shrinePurchaseInteraction.Networkcost = purchaseInteraction.cost;
                        shrinePurchaseInteraction.costType = purchaseInteraction.costType;
                    }
                    else
                    {
                        // Of the original doesn't have a purchase interaction, default to no cost
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
                if (esm.state is EntityStates.Barrel.Opened)
                {
#if DEBUG
                    Log.Debug($"Skipping opened chest {interactableObject}");
#endif
                    yield break;
                }
            }

            PurchaseInteraction purchaseInteraction = interactableObject.GetComponent<PurchaseInteraction>();

            PickupDropTable dropTable;
            PickupIndex[] rolledPickups = Array.Empty<PickupIndex>();
            bool useForcedPickupDropTable = false;

            if (interactableObject.TryGetComponent(out ChestBehavior chestBehavior))
            {
                dropTable = chestBehavior.dropTable;

                if (chestBehavior.dropPickup.isValid)
                {
                    rolledPickups = new PickupIndex[] { chestBehavior.dropPickup };
                }
            }
            else if (interactableObject.TryGetComponent(out RouletteChestController rouletteChestController))
            {
                dropTable = rouletteChestController.dropTable;

#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                RouletteChestController.Entry[] entries = rouletteChestController.entries;
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
                if (entries != null && entries.Length > 0)
                {
                    rolledPickups = Array.ConvertAll(entries, e => e.pickupIndex);
                }
                else
                {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                    Xoroshiro128Plus lootRNG = new Xoroshiro128Plus(0) { state0 = rouletteChestController.rng.state0, state1 = rouletteChestController.rng.state1 };
#pragma warning restore Publicizer001 // Accessing a member that was not originally public

                    rolledPickups = new PickupIndex[rouletteChestController.maxEntries];

                    PickupIndex lastPickup = PickupIndex.none;
                    for (int i = 0; i < rolledPickups.Length; i++)
                    {
                        PickupIndex pickupIndex = dropTable.GenerateDrop(lootRNG);
                        if (pickupIndex == lastPickup)
                            pickupIndex = dropTable.GenerateDrop(lootRNG);

                        rolledPickups[i] = pickupIndex;
                        lastPickup = pickupIndex;
                    }
                }
            }
            else if (interactableObject.TryGetComponent(out ShopTerminalBehavior shopTerminalBehavior))
            {
                if (!shopTerminalBehavior.NetworkpickupIndex.isValid)
                {
#if DEBUG
                    Log.Debug($"Skipping closed shop terminal {interactableObject}");
#endif
                    yield break;
                }

                dropTable = shopTerminalBehavior.dropTable;

                PickupIndex pickup = shopTerminalBehavior.CurrentPickupIndex();
                if (pickup.isValid)
                {
                    rolledPickups = new PickupIndex[] { pickup };
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
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                dropTable = optionChestBehavior.dropTable;

                if (optionChestBehavior.generatedDrops != null)
                {
                    rolledPickups = optionChestBehavior.generatedDrops;
                }
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
            }
            else
            {
#if DEBUG
                Log.Debug($"No usable component found on interactable {interactableObject}");
#endif
                yield break;
            }

            yield return new ShrineReplacementData(interactableObject, dropTable, true, rolledPickups, useForcedPickupDropTable);
        }
    }
}
