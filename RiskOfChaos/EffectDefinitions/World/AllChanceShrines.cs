using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RoR2;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosEffect("all_chance_shrines", DefaultSelectionWeight = 0.7f)]
    public sealed class AllChanceShrines : BaseEffect
    {
        static InteractableSpawnCard _iscChanceShrine;

        [SystemInitializer]
        static void Init()
        {
            InteractableSpawnCard iscChanceShrine = Addressables.LoadAssetAsync<InteractableSpawnCard>("RoR2/Base/ShrineChance/iscShrineChance.asset").WaitForCompletion();

            // Make new instance of the spawn card so that settings can safely be changed without messing with the original behavior
            _iscChanceShrine = ScriptableObject.Instantiate(iscChanceShrine);
            
            // Make sure it'll always spawn no matter what
            _iscChanceShrine.skipSpawnWhenSacrificeArtifactEnabled = false;

            // Make sure the shrine will be spawned at the exact position and rotation given
            _iscChanceShrine.orientToFloor = false;
            _iscChanceShrine.slightlyRandomizeOrientation = false;

            // Prevent random rotation around local y axis
            IL.RoR2.InteractableSpawnCard.Spawn += il =>
            {
                ILCursor c = new ILCursor(il);

                ILLabel patchLocationLbl = null;
                if (c.TryGotoNext(MoveType.After,
                                  x => x.MatchLdfld<InteractableSpawnCard>(nameof(InteractableSpawnCard.orientToFloor)),
                                  x => x.MatchBrfalse(out patchLocationLbl)))
                {
                    c.Goto(patchLocationLbl.Target, MoveType.Before);

                    int beforeDelegateIndex = c.Index;

                    c.Emit(OpCodes.Ldarg_0);
                    c.EmitDelegate((InteractableSpawnCard instance) =>
                    {
                        return instance != _iscChanceShrine;
                    });

                    ILLabel afterRotateLbl = c.DefineLabel();

                    c.Emit(OpCodes.Brfalse, afterRotateLbl);

                    int afterDelegateIndex = c.Index;

                    c.Goto(beforeDelegateIndex, MoveType.Before);
                    patchLocationLbl.Target = c.Next;

                    c.Index = afterDelegateIndex;

                    if (c.TryGotoNext(MoveType.After,
                                      x => x.MatchCallOrCallvirt(SymbolExtensions.GetMethodInfo<Transform>(_ => _.Rotate(default(Vector3), default, default)))))
                    {
                        afterRotateLbl.Target = c.Next;
                    }
                    else
                    {
                        Log.Error($"Failed to find {nameof(afterRotateLbl)} target location");
                    }
                }
                else
                {
                    Log.Warning("Failed to find patch location");
                }
            };
        }

        readonly struct ShrineReplacementData
        {
            public readonly GameObject OriginalObject;
            public readonly Transform OriginalObjectTransform;
            public readonly PickupDropTable DropTable;

            public readonly bool ShouldSpawnShrine;

            ShrineReplacementData(GameObject originalObject, PickupDropTable dropTable, bool shouldSpawnShrine)
            {
                OriginalObject = originalObject;
                OriginalObjectTransform = OriginalObject.transform;
                DropTable = dropTable;
                ShouldSpawnShrine = shouldSpawnShrine;
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
                    placementMode = DirectorPlacementRule.PlacementMode.Direct
                };

                SpawnCard.SpawnResult spawnResult = _iscChanceShrine.DoSpawn(OriginalObjectTransform.position, OriginalObjectTransform.rotation, new DirectorSpawnRequest(_iscChanceShrine, placementRule, rng));

                if (spawnResult.success && spawnResult.spawnedInstance && spawnResult.spawnedInstance.TryGetComponent(out ShrineChanceBehavior shrineChanceBehavior))
                {
                    if (DropTable)
                    {
                        shrineChanceBehavior.dropTable = DropTable;
                    }
                    else
                    {
                        Log.Warning($"null dropTable for interactable {OriginalObject}, not overriding");
                    }

                    if (OriginalObject.TryGetComponent(out PurchaseInteraction purchaseInteraction) &&
                        shrineChanceBehavior.TryGetComponent(out PurchaseInteraction shrinePurchaseInteraction))
                    {
                        shrinePurchaseInteraction.Networkcost = purchaseInteraction.cost;
                        shrinePurchaseInteraction.costType = purchaseInteraction.costType;
                    }

                    NetworkServer.Destroy(OriginalObject);
                }
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

                if (interactableObject.TryGetComponent(out ChestBehavior chestBehavior))
                {
                    dropTable = chestBehavior.dropTable;
                }
                else if (interactableObject.TryGetComponent(out RouletteChestController rouletteChestController))
                {
                    dropTable = rouletteChestController.dropTable;
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

                    if (shopTerminalBehavior.serverMultiShopController)
                    {
                        yield return new ShrineReplacementData(shopTerminalBehavior.serverMultiShopController.gameObject, null, false);
                    }
                }
                else if (interactableObject.TryGetComponent(out OptionChestBehavior optionChestBehavior))
                {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                    dropTable = optionChestBehavior.dropTable;
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
                }
                else
                {
#if DEBUG
                    Log.Debug($"No usable component found on interactable {interactableObject}");
#endif
                    yield break;
                }

                yield return new ShrineReplacementData(interactableObject, dropTable, true);
            }
        }

        static IEnumerable<ShrineReplacementData> getAllReplacementsData()
        {
            // HACK: ToArray is used to avoid an InvalidOperationException due to foreach modifying the collection by destroying the purchase interactions
            return InstanceTracker.GetInstancesList<PurchaseInteraction>().ToArray().SelectMany(ShrineReplacementData.GetReplacementDatasFor);
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _iscChanceShrine && getAllReplacementsData().Any();
        }

        public override void OnStart()
        {
            foreach (ShrineReplacementData replacementData in getAllReplacementsData())
            {
                replacementData.PerformReplacement(new Xoroshiro128Plus(RNG.nextUlong));
            }
        }
    }
}
