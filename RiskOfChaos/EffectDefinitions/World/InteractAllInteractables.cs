﻿using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosEffect("interact_all_interactables", DefaultSelectionWeight = 0.4f)]
    public sealed class InteractAllInteractables : BaseEffect
    {
        static bool canBeOpened(IInteractable interactable)
        {
            if (interactable is not Component interactableComponent)
                return false;

            if (interactableComponent.TryGetComponent(out EntityStateMachine esm))
            {
                if (esm.state is EntityStates.Barrel.Opened or EntityStates.Interactables.GoldBeacon.NotReady)
                {
                    return false;
                }
            }

            if (interactableComponent.TryGetComponent(out ShopTerminalBehavior shopTerminalBehavior))
            {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                if (shopTerminalBehavior.hasBeenPurchased)
                    return false;
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
            }

            return true;
        }

        static IEnumerable<IInteractable> getAllValidInteractables()
        {
            static bool isPurchaseInteractionValid(PurchaseInteraction purchaseInteraction)
            {
                return !purchaseInteraction.GetComponent<ShrineRestackBehavior>();
            }

            return InstanceTracker.GetInstancesList<PurchaseInteraction>().Where(isPurchaseInteractionValid)
                                  .Cast<IInteractable>()
                                  .Concat(InstanceTracker.GetInstancesList<BarrelInteraction>())
                                  .Concat(InstanceTracker.GetInstancesList<TimedChestController>())
                                  .Where(canBeOpened);
        }

        static IEnumerable<Interactor> getInteractors()
        {
            return PlayerUtils.GetAllPlayerBodies(true).Select(c => c.GetComponent<Interactor>()).Where(i => i);
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return getInteractors().Any() && getAllValidInteractables().Any();
        }

        EquipmentIndex _fallbackEquipment = EquipmentIndex.None;

        public override void OnPreStartServer()
        {
            base.OnPreStartServer();

            List<PickupIndex> availableEquipmentDropList = Run.instance.availableEquipmentDropList;
            if (availableEquipmentDropList != null && availableEquipmentDropList.Count > 0)
            {
                _fallbackEquipment = PickupCatalog.GetPickupDef(RNG.NextElementUniform(availableEquipmentDropList)).equipmentIndex;
            }
        }

        public override void OnStart()
        {
            Interactor[] interactors = getInteractors().ToArray();

            On.RoR2.CostTypeDef.IsAffordable += CostTypeDef_IsAffordable;
            On.RoR2.CostTypeDef.PayCost += CostTypeDef_PayCost;

            getAllValidInteractables().ToList().TryDo(interactable =>
            {
                if (interactable is not Component interactableComponent)
                    return;

                PurchaseInteraction purchaseInteraction = interactableComponent.GetComponent<PurchaseInteraction>();

                if (interactableComponent.TryGetComponent(out ShopTerminalBehavior shopTerminalBehavior))
                {
                    if (shopTerminalBehavior.serverMultiShopController)
                    {
                        shopTerminalBehavior.serverMultiShopController.SetCloseOnTerminalPurchase(purchaseInteraction, false);
                    }
                }

                if (!canBeOpened(interactable))
                    return;

                Interactor interactor = RNG.NextElementUniform(interactors);

                interactable.OnInteractionBegin(interactor);
            });

            On.RoR2.CostTypeDef.IsAffordable -= CostTypeDef_IsAffordable;
            On.RoR2.CostTypeDef.PayCost -= CostTypeDef_PayCost;
        }

        static bool CostTypeDef_IsAffordable(On.RoR2.CostTypeDef.orig_IsAffordable orig, CostTypeDef self, int cost, Interactor activator)
        {
            return true;
        }

        CostTypeDef.PayCostResults CostTypeDef_PayCost(On.RoR2.CostTypeDef.orig_PayCost orig, CostTypeDef self, int cost, Interactor activator, GameObject purchasedObject, Xoroshiro128Plus rng, ItemIndex avoidedItemIndex)
        {
            CostTypeDef.PayCostResults results = new CostTypeDef.PayCostResults();

            // Still include equipment data in cost results, but don't remove from inventory
            // If this isn't done equipment drones will not spawn at all
            if (self == CostTypeCatalog.GetCostTypeDef(CostTypeIndex.Equipment) || self == CostTypeCatalog.GetCostTypeDef(CostTypeIndex.VolatileBattery))
            {
                if (activator)
                {
                    CharacterBody activatorBody = activator.GetComponent<CharacterBody>();
                    if (activatorBody && activatorBody.inventory)
                    {
                        EquipmentIndex equipment = activatorBody.inventory.GetEquipmentIndex();

                        if (equipment == EquipmentIndex.None)
                            equipment = _fallbackEquipment;

                        if (equipment != EquipmentIndex.None)
                        {
                            results.equipmentTaken.Add(equipment);
                        }
                    }
                }
            }

            return results;
        }
    }
}