using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Patches;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.Orbs;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Items
{
    [ChaosEffect("swap_player_inventories")]
    public sealed class SwapPlayerInventories : BaseEffect
    {
        [RequireComponent(typeof(CharacterBody))]
        class GiveInventoryTo : MonoBehaviour
        {
            public CharacterBody OwnerBody;
            public Inventory OwnerInventory;

            public CharacterBody Target;
            Inventory _targetInventory;

            ItemStack[] _itemStacksToTransfer = Array.Empty<ItemStack>();
            int _currentItemTransferIndex;

            float _giveNextItemTimer;

            readonly List<ItemTransferOrb> _inFlightOrbs = new List<ItemTransferOrb>();

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
                    List<ItemStack> itemsToTransfer = new List<ItemStack>();
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

                OnFinishGivingInventory?.Invoke();
            }

            void FixedUpdate()
            {
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
                    Destroy(this);
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
        }

        static IEnumerable<CharacterBody> getAllEligiblePlayers()
        {
            return PlayerUtils.GetAllPlayerBodies(true).Where(b => b.inventory.itemAcquisitionOrder.Any(GiveInventoryTo.ItemTransferFilter));
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return getAllEligiblePlayers().CountGreaterThanOrEqualTo(2);
        }

        public override void OnStart()
        {
            CharacterBody[] playerBodies = getAllEligiblePlayers().ToArray();
            Util.ShuffleArray(playerBodies, RNG);

            GiveInventoryTo[] inventoryGiverControllers = new GiveInventoryTo[playerBodies.Length];

            for (int i = 0; i < playerBodies.Length; i++)
            {
                GiveInventoryTo giveInventoryTo = playerBodies[i].gameObject.AddComponent<GiveInventoryTo>();
                CharacterBody targetBody = playerBodies[(i + 1) % playerBodies.Length];
                giveInventoryTo.Target = targetBody;

                inventoryGiverControllers[i] = giveInventoryTo;
            }

            foreach (GiveInventoryTo giveInventoryTo in inventoryGiverControllers)
            {
                Inventory inventory = giveInventoryTo.OwnerInventory;
                if (inventory)
                {
                    EventWaiter eventWaiter = new EventWaiter();

                    giveInventoryTo.OnFinishGivingInventory += eventWaiter.GetListener();

                    foreach (GiveInventoryTo givingInventoryToUs in inventoryGiverControllers.Where(g => g.Target == giveInventoryTo.OwnerBody))
                    {
                        givingInventoryToUs.OnFinishGivingInventory += eventWaiter.GetListener();
                    }

                    IgnoreItemTransformations.IgnoreTransformationsFor(inventory);
                    eventWaiter.OnAllEventsInvoked += () =>
                    {
                        IgnoreItemTransformations.ResumeTransformationsFor(inventory);
                    };
                }
            }
        }
    }
}
