using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Networking;
using RiskOfChaos.Trackers;
using RoR2;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosEffect("all_chance_shrines", DefaultSelectionWeight = 0.7f, EffectActivationCountHardCap = 1)]
    public class AllChanceShrines : BaseEffect
    {
        static readonly InteractableSpawnCard _iscChanceShrine = Addressables.LoadAssetAsync<InteractableSpawnCard>("RoR2/Base/ShrineChance/iscShrineChance.asset").WaitForCompletion();

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _iscChanceShrine && InstanceTracker.GetInstancesList<InteractableTracker>().Count > 0;
        }

        public override void OnStart()
        {
            List<InteractableTracker> interactables = new List<InteractableTracker>(InstanceTracker.GetInstancesList<InteractableTracker>());
            foreach (InteractableTracker interactable in interactables)
            {
                tryReplaceInteractable(interactable.gameObject);
            }
        }

        bool tryReplaceInteractable(GameObject interactableObject)
        {
            if (interactableObject.TryGetComponent(out EntityStateMachine esm))
            {
                if (esm.state is EntityStates.Barrel.Opened)
                {
#if DEBUG
                    Log.Debug($"Skipping opened chest {interactableObject}");
#endif
                    return false;
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
                    Log.Debug($"Skipping likely closed shop terminal {interactableObject}");
#endif
                    return false;
                }

                dropTable = shopTerminalBehavior.dropTable;
            }
            else if (interactableObject.TryGetComponent(out OptionChestBehavior optionChestBehavior))
            {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                dropTable = optionChestBehavior.dropTable;
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
            }
            else if (interactableObject.TryGetComponent(out MultiShopController multiShopController))
            {
                bool allReplaced = true;
                bool anyReplaced = false;

                foreach (GameObject terminalObject in multiShopController.terminalGameObjects)
                {
                    bool replaced = tryReplaceInteractable(terminalObject);
                    allReplaced &= replaced;
                    anyReplaced |= replaced;
                }

                if (allReplaced)
                {
                    NetworkServer.Destroy(interactableObject);
                }

                return anyReplaced;
            }
            else
            {
#if DEBUG
                Log.Debug($"No usable component found on interactable {interactableObject}");
#endif
                return false;
            }

            DirectorPlacementRule placementRule = new DirectorPlacementRule
            {
                placementMode = DirectorPlacementRule.PlacementMode.Direct
            };

            SpawnCard.SpawnResult spawnResult = _iscChanceShrine.DoSpawn(interactableObject.transform.position, interactableObject.transform.rotation, new DirectorSpawnRequest(_iscChanceShrine, placementRule, RNG));

            if (spawnResult.success && spawnResult.spawnedInstance && spawnResult.spawnedInstance.TryGetComponent(out ShrineChanceBehavior shrineChanceBehavior))
            {
                if (dropTable)
                {
                    shrineChanceBehavior.dropTable = dropTable;
                }
                else
                {
                    Log.Warning($"null dropTable for interactable {interactableObject}, not overriding");
                }

                if (purchaseInteraction && shrineChanceBehavior.TryGetComponent(out PurchaseInteraction shrinePurchaseInteraction))
                {
                    shrinePurchaseInteraction.cost = purchaseInteraction.cost;
                    SyncPurchaseInteractionCostType.SetCostTypeNetworked(shrinePurchaseInteraction, purchaseInteraction.costType);
                }

                NetworkServer.Destroy(interactableObject);
                return true;
            }

            return false;
        }
    }
}
