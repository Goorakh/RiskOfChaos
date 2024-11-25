using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Interactables
{
    [ChaosEffect("interact_all_interactables", DefaultSelectionWeight = 0.5f)]
    public sealed class InteractAllInteractables : NetworkBehaviour
    {
        [EffectConfig]
        static readonly ConfigHolder<bool> _allowOrderShrineActivation =
            ConfigFactory<bool>.CreateConfig("Allow Shrine of Order Activation", false)
                               .Description("If Shrines of Order can be activated by the effect")
                               .OptionConfig(new CheckBoxConfig())
                               .Build();

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
                if (shopTerminalBehavior.hasBeenPurchased)
                    return false;
            }

            return true;
        }

        static IEnumerable<IInteractable> getAllValidInteractables()
        {
            static bool isPurchaseInteractionValid(PurchaseInteraction purchaseInteraction)
            {
                return _allowOrderShrineActivation.Value || !purchaseInteraction.GetComponent<ShrineRestackBehavior>();
            }

            static bool isGenericInteractionValid(GenericInteraction genericInteraction)
            {
                return !genericInteraction.GetComponent<SceneExitController>();
            }

            return InstanceTracker.GetInstancesList<PurchaseInteraction>().Where(isPurchaseInteractionValid)
                                  .Cast<IInteractable>()
                                  .Concat(InstanceTracker.GetInstancesList<AurelioniteHeartController>())
                                  .Concat(InstanceTracker.GetInstancesList<BarrelInteraction>())
                                  .Concat(InstanceTracker.GetInstancesList<GenericInteraction>().Where(isGenericInteractionValid))
                                  .Concat(InstanceTracker.GetInstancesList<GeodeController>())
                                  .Concat(InstanceTracker.GetInstancesList<ProxyInteraction>())
                                  .Concat(InstanceTracker.GetInstancesList<TimedChestController>())
                                  .Concat(InstanceTracker.GetInstancesList<WokController>())
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

        ChaosEffectComponent _effectComponent;

        Xoroshiro128Plus _rng;

        EquipmentIndex _fallbackEquipment = EquipmentIndex.None;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            _rng = new Xoroshiro128Plus(_effectComponent.Rng.nextUlong);

            List<PickupIndex> availableEquipmentDropList = Run.instance.availableEquipmentDropList;
            if (availableEquipmentDropList != null && availableEquipmentDropList.Count > 0)
            {
                _fallbackEquipment = PickupCatalog.GetPickupDef(_rng.NextElementUniform(availableEquipmentDropList)).equipmentIndex;
            }
            else
            {
                _fallbackEquipment = RoR2Content.Equipment.Scanner.equipmentIndex;
            }
        }

        void Start()
        {
            if (!NetworkServer.active)
                return;

            Interactor[] interactors = getInteractors().ToArray();

            On.RoR2.CostTypeDef.IsAffordable += CostTypeDef_IsAffordable;
            On.RoR2.CostTypeDef.PayCost += CostTypeDef_PayCost;

            getAllValidInteractables().ToList().TryDo(interactable =>
            {
                // Duplicate check, but may be non-available since the list was generated
                if (!canBeOpened(interactable))
                    return;

                Component interactableComponent = (Component)interactable;

                PurchaseInteraction purchaseInteraction = interactableComponent.GetComponent<PurchaseInteraction>();

                if (interactableComponent.TryGetComponent(out ShopTerminalBehavior shopTerminalBehavior))
                {
                    if (shopTerminalBehavior.serverMultiShopController)
                    {
                        shopTerminalBehavior.serverMultiShopController.SetCloseOnTerminalPurchase(purchaseInteraction, false);
                    }
                }

                Interactor interactor = _rng.NextElementUniform(interactors);

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
