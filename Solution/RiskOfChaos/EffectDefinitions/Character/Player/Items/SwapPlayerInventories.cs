using RiskOfChaos.Content.Orbs;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Patches;
using RiskOfChaos.Utilities;
using RoR2;
using RoR2.Orbs;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Items
{
    [ChaosEffect("swap_player_inventories", EnabledInSingleplayer = false)]
    public sealed class SwapPlayerInventories : NetworkBehaviour
    {
        [RequireComponent(typeof(CharacterBody))]
        class GiveInventoryTo : MonoBehaviour
        {
            public CharacterBody OwnerBody;
            public Inventory OwnerInventory;

            public CharacterBody Target;
            Inventory _targetInventory;

            ItemStack[] _itemStacksToTransfer = [];
            int _currentItemTransferIndex;

            bool _hasFinishedGivingItems;

            float _giveNextItemTimer;

            readonly List<ItemTransferOrb> _inFlightOrbs = [];

            public static bool ItemTransferFilter(ItemIndex item)
            {
                ItemDef itemDef = ItemCatalog.GetItemDef(item);
                return itemDef && !itemDef.hidden;
            }

            public event Action OnFinishGivingInventory;

            void Awake()
            {
                OwnerBody = GetComponent<CharacterBody>();
                OwnerInventory = OwnerBody.inventory;

                scheduleGiveNextItem();
            }

            void Start()
            {
                _targetInventory = Target.inventory;

                if (OwnerInventory)
                {
                    List<ItemStack> itemsToTransfer = [];
                    foreach (ItemIndex item in OwnerInventory.itemAcquisitionOrder)
                    {
                        if (ItemTransferFilter(item))
                        {
                            itemsToTransfer.Add(new ItemStack(item, OwnerInventory.GetItemCount(item)));
                        }
                    }

                    _itemStacksToTransfer = itemsToTransfer.ToArray();
                }
            }

            void OnDestroy()
            {
                if (OrbManager.instance)
                {
                    for (int i = _inFlightOrbs.Count - 1; i >= 0; i--)
                    {
                        OrbManager.instance.ForceImmediateArrival(_inFlightOrbs[i]);
                    }
                }

                if (_currentItemTransferIndex < _itemStacksToTransfer.Length && OwnerInventory && _targetInventory)
                {
                    for (int i = _currentItemTransferIndex; i < _itemStacksToTransfer.Length; i++)
                    {
                        ItemStack itemStack = _itemStacksToTransfer[i];

                        OwnerInventory.RemoveItem(itemStack.ItemIndex, itemStack.ItemCount);
                        _targetInventory.GiveItem(itemStack.ItemIndex, itemStack.ItemCount);
                    }
                }

                _hasFinishedGivingItems = true;
                OnFinishGivingInventory?.Invoke();
            }

            void FixedUpdate()
            {
                if (_hasFinishedGivingItems)
                    return;

                if (_currentItemTransferIndex < _itemStacksToTransfer.Length)
                {
                    _giveNextItemTimer -= Time.fixedDeltaTime;
                    if (_giveNextItemTimer <= 0f)
                    {
                        giveNextItem();
                        scheduleGiveNextItem();
                    }
                }
                else if (_inFlightOrbs.Count == 0)
                {
                    _hasFinishedGivingItems = true;
                    OnFinishGivingInventory?.Invoke();
                }
            }

            void scheduleGiveNextItem()
            {
                _giveNextItemTimer = 0.2f * Mathf.Clamp(Mathf.Exp(-((_currentItemTransferIndex - 5) / 30f)), 0.1f, 1f);
            }

            void giveNextItem()
            {
                ItemStack itemStack = _itemStacksToTransfer[_currentItemTransferIndex++];
                OwnerInventory.RemoveItem(itemStack.ItemIndex, itemStack.ItemCount);
                ItemTransferOrb orb = ItemTransferOrb.DispatchItemTransferOrb(OwnerBody.corePosition, Target.inventory, itemStack.ItemIndex, itemStack.ItemCount, orb =>
                {
                    ItemTransferOrb.DefaultOnArrivalBehavior(orb);
                    _inFlightOrbs.Remove(orb);
                });

                _inFlightOrbs.Add(orb);
            }

            public void TransferEquipment()
            {
                EquipmentIndex currentEquipment = OwnerInventory.GetEquipmentIndex();
                if (currentEquipment == EquipmentIndex.None)
                    return;

                OwnerInventory.SetEquipmentIndex(EquipmentIndex.None);
                EquipmentTransferOrb.DispatchEquipmentTransferOrb(OwnerBody.corePosition, _targetInventory, currentEquipment);
            }
        }

        static List<CharacterBody> getEligiblePlayerBodies()
        {
            List<CharacterBody> playerBodies = new List<CharacterBody>(PlayerCharacterMasterController.instances.Count);
            foreach (PlayerCharacterMasterController playerMasterController in PlayerCharacterMasterController.instances)
            {
                if (playerMasterController.isConnected)
                {
                    CharacterMaster master = playerMasterController.master;
                    CharacterBody body = master ? master.GetBody() : null;
                    HealthComponent healthComponent = body ? body.healthComponent : null;
                    if (healthComponent && healthComponent.alive)
                    {
                        playerBodies.Add(body);
                    }
                }
            }

            return playerBodies;
        }

        [EffectCanActivate]
        static bool CanActivate(in EffectCanActivateContext context)
        {
            List<CharacterBody> playerBodies = getEligiblePlayerBodies();
            if (playerBodies.Count < 2)
                return false;

            return !context.IsNow || playerBodies.Any(b => b.inventory && (b.inventory.GetEquipmentIndex() != EquipmentIndex.None || b.inventory.itemAcquisitionOrder.Any(GiveInventoryTo.ItemTransferFilter)));
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
            if (NetworkServer.active)
            {
                List<CharacterBody> playerBodies = getEligiblePlayerBodies();
                if (playerBodies.Count < 2)
                    return;

                Util.ShuffleList(playerBodies, _rng);

                GiveInventoryTo[] inventoryGiverControllers = new GiveInventoryTo[playerBodies.Count];

                for (int i = 0; i < playerBodies.Count; i++)
                {
                    GiveInventoryTo giveInventoryTo = playerBodies[i].gameObject.AddComponent<GiveInventoryTo>();
                    CharacterBody targetBody = playerBodies[(i + 1) % playerBodies.Count];
                    giveInventoryTo.Target = targetBody;

                    inventoryGiverControllers[i] = giveInventoryTo;
                }

                EventWaiter allItemsTransferredWaiter = new EventWaiter();

                foreach (GiveInventoryTo inventoryGiver in inventoryGiverControllers)
                {
                    inventoryGiver.OnFinishGivingInventory += allItemsTransferredWaiter.GetListener();

                    Inventory inventory = inventoryGiver.OwnerInventory;
                    if (inventory)
                    {
                        EventWaiter allItemsGivenAndReceivedWaiter = new EventWaiter();

                        inventoryGiver.OnFinishGivingInventory += allItemsGivenAndReceivedWaiter.GetListener();

                        foreach (GiveInventoryTo inventoryGiveController in inventoryGiverControllers)
                        {
                            if (inventoryGiveController.Target == inventoryGiver.OwnerBody)
                            {
                                inventoryGiveController.OnFinishGivingInventory += allItemsGivenAndReceivedWaiter.GetListener();
                            }
                        }

                        IgnoreItemTransformations.IgnoreTransformationsFor(inventory);
                        allItemsGivenAndReceivedWaiter.OnAllEventsInvoked += () =>
                        {
                            IgnoreItemTransformations.ResumeTransformationsFor(inventory);
                        };
                    }
                }

                allItemsTransferredWaiter.OnAllEventsInvoked += () =>
                {
                    foreach (GiveInventoryTo giveInventoryTo in inventoryGiverControllers)
                    {
                        if (giveInventoryTo)
                        {
                            giveInventoryTo.TransferEquipment();
                        }
                    }

                    foreach (GiveInventoryTo giveInventoryTo in inventoryGiverControllers)
                    {
                        GameObject.Destroy(giveInventoryTo);
                    }
                };
            }
        }
    }
}
