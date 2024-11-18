using RoR2;
using UnityEngine;

namespace RiskOfChaos.Utilities.Extensions
{
    public static class InventoryExtensions
    {
        public enum ItemReplacementRule
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

        public static bool TryGrant(this Inventory inventory, PickupIndex pickup, ItemReplacementRule replacementRule, int count = 1)
        {
            return TryGrant(inventory, PickupCatalog.GetPickupDef(pickup), replacementRule, count);
        }

        public static bool TryGrant(this Inventory inventory, PickupDef pickup, ItemReplacementRule replacementRule, int count = 1)
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
                uint[] equipmentSlots = new uint[equipmentSlotCount];
                int equipmentSlotIndex = 0;

                for (uint i = 0; i < equipmentSlotCount; i++)
                {
                    if (i == activeEquipmentSlot)
                        continue;

                    if (inventory.GetEquipment(i).equipmentIndex == EquipmentIndex.None)
                    {
                        equipmentSlots[equipmentSlotIndex++] = i;
                    }
                }

                equipmentSlots[equipmentSlotIndex++] = activeEquipmentSlot;

                for (uint i = 0; i < equipmentSlotCount; i++)
                {
                    if (i == activeEquipmentSlot)
                        continue;

                    if (inventory.GetEquipment(i).equipmentIndex != EquipmentIndex.None)
                    {
                        equipmentSlots[equipmentSlotIndex++] = i;
                    }
                }

                Vector3? pickupSpawnPosition = null;

                if (inventory.TryGetComponent(out CharacterMaster characterMaster))
                {
                    CharacterBody body = characterMaster.GetBody();
                    if (body)
                    {
                        pickupSpawnPosition = body.footPosition;
                    }
                }

                for (int i = 0; grantedCount < count && i < equipmentSlotCount; i++)
                {
                    uint equipmentSlot = equipmentSlots[i];
                    EquipmentState currentEquipmentState = inventory.GetEquipment(equipmentSlot);

                    if (currentEquipmentState.equipmentIndex != EquipmentIndex.None)
                    {
                        if (replacementRule == ItemReplacementRule.KeepExisting)
                            continue;

                        if (replacementRule == ItemReplacementRule.DropExisting)
                        {
                            if (pickupSpawnPosition.HasValue)
                            {
                                GenericPickupController.CreatePickup(new GenericPickupController.CreatePickupInfo
                                {
                                    pickupIndex = PickupCatalog.FindPickupIndex(currentEquipmentState.equipmentIndex),
                                    position = pickupSpawnPosition.Value
                                });
                            }
                            else
                            {
                                Log.Warning($"No position could be determined for dropping existing equipment ({currentEquipmentState.equipmentDef}) for {inventory}");
                            }
                        }
                    }

                    inventory.SetEquipmentIndexForSlot(pickup.equipmentIndex, equipmentSlot);
                    grantedCount++;
                }

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
    }
}
