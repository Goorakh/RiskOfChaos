using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfChaos.Utilities.Extensions
{
    public static class InventoryExtensions
    {
        public enum EquipmentReplacementRule
        {
            KeepExisting,
            DeleteExisting,
            DropExisting
        }

        public static bool TryGetFreeEquipmentSlot(this Inventory inventory, out uint slotIndex)
        {
            if (!inventory)
            {
                slotIndex = default;
                return false;
            }

            byte activeEquipmentSlot = inventory.activeEquipmentSlot;
            if (inventory.GetEquipment(activeEquipmentSlot).equipmentIndex == EquipmentIndex.None)
            {
                slotIndex = activeEquipmentSlot;
                return true;
            }

            int slotCount = inventory.GetEquipmentSlotCount();
            for (slotIndex = 0; slotIndex < slotCount; slotIndex++)
            {
                if (slotIndex != activeEquipmentSlot && inventory.GetEquipment(slotIndex).equipmentIndex == EquipmentIndex.None)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool TryGrant(this Inventory inventory, PickupIndex pickup, EquipmentReplacementRule equipmentReplacementRule, int count = 1)
        {
            return TryGrant(inventory, PickupCatalog.GetPickupDef(pickup), equipmentReplacementRule, count);
        }

        public static bool TryGrant(this Inventory inventory, PickupDef pickup, EquipmentReplacementRule equipmentReplacementRule, int count = 1)
        {
            if (!inventory || pickup == null)
                return false;

            if (pickup.itemIndex != ItemIndex.None)
            {
                inventory.GiveItem(pickup.itemIndex, count);
                return true;
            }
            else if (pickup.equipmentIndex != EquipmentIndex.None)
            {
                int grantedCount = 0;

                byte activeEquipmentSlot = inventory.activeEquipmentSlot;

                int equipmentSlotCount = inventory.GetEquipmentSlotCount();

                List<uint> emptyEquipmentSlots = new List<uint>(equipmentSlotCount);
                List<uint> nonEmptyEquipmentSlots = new List<uint>(equipmentSlotCount);

                List<uint> getEquipmentSlotList(uint slot)
                {
                    bool isEmpty = inventory.GetEquipment(slot).equipmentIndex == EquipmentIndex.None;

                    return isEmpty ? emptyEquipmentSlots : nonEmptyEquipmentSlots;
                }

                for (uint i = 0; i < equipmentSlotCount; i++)
                {
                    if (i != activeEquipmentSlot)
                    {
                        getEquipmentSlotList(i).Add(i);
                    }
                }

                getEquipmentSlotList(activeEquipmentSlot).Insert(0, activeEquipmentSlot);

                uint[] equipmentSlots = [.. emptyEquipmentSlots, .. nonEmptyEquipmentSlots];

                Log.Debug($"{inventory} sorted equipmentSlots: [{string.Join(", ", equipmentSlots)}]");

                Vector3 pickupSpawnPosition = inventory.transform.position;
                if (inventory.TryGetComponent(out CharacterMaster characterMaster))
                {
                    CharacterBody body = characterMaster.GetBody();
                    if (body)
                    {
                        pickupSpawnPosition = body.corePosition;
                    }
                }

                for (int i = 0; grantedCount < count && i < equipmentSlots.Length; i++)
                {
                    uint equipmentSlot = equipmentSlots[i];
                    EquipmentState currentEquipmentState = inventory.GetEquipment(equipmentSlot);

                    if (currentEquipmentState.equipmentIndex != EquipmentIndex.None)
                    {
                        switch (equipmentReplacementRule)
                        {
                            case EquipmentReplacementRule.KeepExisting:
                                continue;
                            case EquipmentReplacementRule.DeleteExisting:
                                break;
                            case EquipmentReplacementRule.DropExisting:
                                PickupDropletController.CreatePickupDroplet(PickupCatalog.FindPickupIndex(currentEquipmentState.equipmentIndex), pickupSpawnPosition, Vector3.up * 15f);
                                break;
                        }
                    }

                    Log.Debug($"Setting equipment for {inventory} in slot {equipmentSlot} to {FormatUtils.GetBestEquipmentDisplayName(pickup.equipmentIndex)}");

                    inventory.SetEquipmentIndexForSlot(pickup.equipmentIndex, equipmentSlot);
                    grantedCount++;
                }

                Log.Debug($"Granted {grantedCount}/{count} equipment(s) to {inventory}");

                return grantedCount > 0;
            }
            else
            {
                Log.Error($"Invalid pickup type: {pickup.internalName}");
                return false;
            }
        }

        public static bool TryRemove(this Inventory inventory, PickupIndex pickupIndex, int count = 1)
        {
            return TryRemove(inventory, PickupCatalog.GetPickupDef(pickupIndex), count);
        }

        public static bool TryRemove(this Inventory inventory, PickupDef pickupDef, int count = 1)
        {
            if (!inventory || pickupDef == null)
                return false;

            if (pickupDef.itemIndex != ItemIndex.None)
            {
                inventory.RemoveItem(pickupDef.itemIndex, count);
                return true;
            }
            else if (pickupDef.equipmentIndex != EquipmentIndex.None)
            {
                int numRemoved = 0;

                int equipmentSlotCount = inventory.GetEquipmentSlotCount();
                for (uint i = 0; i < equipmentSlotCount; i++)
                {
                    if (inventory.GetEquipment(i).equipmentIndex == pickupDef.equipmentIndex)
                    {
                        inventory.SetEquipmentIndexForSlot(EquipmentIndex.None, i);

                        numRemoved++;
                        if (numRemoved <= count)
                            break;
                    }
                }

                return numRemoved > 0;
            }
            else
            {
                Log.Warning($"Unhandled pickup index {pickupDef.pickupIndex}");
                return false;
            }
        }

        public static int GetPickupCount(this Inventory inventory, PickupIndex pickupIndex)
        {
            return GetPickupCount(inventory, PickupCatalog.GetPickupDef(pickupIndex));
        }

        public static int GetPickupCount(this Inventory inventory, PickupDef pickupDef)
        {
            if (!inventory || pickupDef == null)
                return 0;

            if (pickupDef.itemIndex != ItemIndex.None)
            {
                return inventory.GetItemCount(pickupDef.itemIndex);
            }
            else if (pickupDef.equipmentIndex != EquipmentIndex.None)
            {
                int equipmentCount = 0;

                int equipmentSlotCount = inventory.GetEquipmentSlotCount();
                for (uint i = 0; i < equipmentSlotCount; i++)
                {
                    if (inventory.GetEquipment(i).equipmentIndex == pickupDef.equipmentIndex)
                    {
                        equipmentCount++;
                    }
                }

                return equipmentCount;
            }
            else
            {
                Log.Warning($"Unhandled pickup index {pickupDef.pickupIndex}");
                return 0;
            }
        }

        public static void EnsureItem(this Inventory inventory, ItemDef item, int count = 1)
        {
            inventory.EnsureItem(item ? item.itemIndex : ItemIndex.None, count);
        }

        public static void EnsureItem(this Inventory inventory, ItemIndex item, int count = 1)
        {
            int currentItemCount = inventory.GetItemCount(item);
            if (currentItemCount < count)
            {
                inventory.GiveItem(item, count - currentItemCount);
            }
        }
    }
}
