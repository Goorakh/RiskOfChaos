using RiskOfChaos.Components;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Patches;
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
                if (shopTerminalBehavior.CurrentPickup() == UniquePickup.none)
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

        [EffectCanActivate]
        static bool CanActivate()
        {
            return getAllValidInteractables().Any();
        }

        ChaosEffectComponent _effectComponent;

        Xoroshiro128Plus _rng;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            _rng = new Xoroshiro128Plus(_effectComponent.Rng.nextUlong);
        }

        void Start()
        {
            if (!NetworkServer.active)
                return;

            List<Interactor> interactors = new List<Interactor>(PlayerCharacterMasterController.instances.Count);
            foreach (PlayerCharacterMasterController playerMaster in PlayerCharacterMasterController.instances)
            {
                if (playerMaster.isConnected && playerMaster.master && !playerMaster.master.IsDeadAndOutOfLivesServer())
                {
                    CharacterBody playerBody = playerMaster.master.GetBody();
                    if (playerBody && playerBody.TryGetComponent(out Interactor playerInteractor))
                    {
                        interactors.Add(playerInteractor);
                    }
                }
            }

            if (interactors.Count == 0)
            {
                Log.Debug("No player interactors available, using chaos interactor");

                Interactor chaosInteractor = ChaosInteractor.GetInteractor();
                if (chaosInteractor)
                {
                    interactors.Add(chaosInteractor);
                }
            }

            Interactor[] availableInteractors = [.. interactors];

            if (availableInteractors.Length == 0)
            {
                Log.Warning("No available interactors");
            }

            CostHooks.OverrideIsAffordable += overrideIsAffordable;
            CostHooks.OverridePayCost += overridePayCost;

            getAllValidInteractables().ToList().TryDo(interactable =>
            {
                // Duplicate check, but may be non-available since the list was generated
                if (!canBeOpened(interactable))
                    return;

                Component interactableComponent = interactable as Component;
                if (interactableComponent)
                {
                    if (interactableComponent.TryGetComponent(out ShopTerminalBehavior shopTerminalBehavior) &&
                        shopTerminalBehavior.serverMultiShopController)
                    {
                        if (interactableComponent.TryGetComponent(out PurchaseInteraction purchaseInteraction))
                        {
                            shopTerminalBehavior.serverMultiShopController.SetCloseOnTerminalPurchase(purchaseInteraction, false);
                        }
                    }
                }

                Interactor interactor = null;
                if (availableInteractors.Length > 0)
                {
                    interactor = _rng.NextElementUniform(availableInteractors);
                }

                interactable.OnInteractionBegin(interactor);
            });

            CostHooks.OverrideIsAffordable -= overrideIsAffordable;
            CostHooks.OverridePayCost -= overridePayCost;
        }

        static void overrideIsAffordable(CostTypeDef costTypeDef, int cost, Interactor activator, ref bool isAffordable)
        {
            isAffordable = true;
        }

        static void overridePayCost(CostTypeDef costTypeDef, CostTypeDef.PayCostContext context, CostTypeDef.PayCostResults result)
        {
            // Still include equipment data in cost results, but don't remove from inventory
            // If this isn't done equipment drones will not spawn at all
            if (costTypeDef == CostTypeCatalog.GetCostTypeDef(CostTypeIndex.Equipment) || costTypeDef == CostTypeCatalog.GetCostTypeDef(CostTypeIndex.VolatileBattery))
            {
                if (result.equipmentTaken.Count == 0)
                {
                    EquipmentIndex equipmentTaken = EquipmentIndex.None;
                    if (context.activatorInventory)
                    {
                        EquipmentIndex activatorCurrentEquipment = context.activatorInventory.GetEquipmentIndex();
                        if (activatorCurrentEquipment != EquipmentIndex.None)
                        {
                            equipmentTaken = activatorCurrentEquipment;
                        }
                    }

                    // EquipmentIndex.None is perfectly fine here, there just needs to be *something* in the list
                    result.equipmentTaken.Add(equipmentTaken);
                }
            }
        }
    }
}
