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

        public static bool TryGrant(this Inventory inventory, PickupIndex pickup, ItemReplacementRule replacementRule)
        {
            return TryGrant(inventory, PickupCatalog.GetPickupDef(pickup), replacementRule);
        }

        public static bool TryGrant(this Inventory inventory, PickupDef pickup, ItemReplacementRule replacementRule)
        {
            if (!inventory || pickup == null)
                return false;

            if (pickup.itemIndex != ItemIndex.None)
            {
                inventory.GiveItem(pickup.itemIndex);
                return true;
            }
            else if (pickup.equipmentIndex != EquipmentIndex.None)
            {
                if (inventory.TryGetFreeEquipmentSlot(out uint freeSlot))
                {
                    inventory.SetEquipmentIndexForSlot(pickup.equipmentIndex, freeSlot);
                    return true;
                }

                byte currentEquipmentSlot = inventory.activeEquipmentSlot;
                EquipmentState currentEquipmentState = inventory.GetEquipment(currentEquipmentSlot);

                if (currentEquipmentState.equipmentIndex != EquipmentIndex.None)
                {
                    if (replacementRule == ItemReplacementRule.KeepExisting)
                        return false;

                    if (replacementRule == ItemReplacementRule.DropExisting)
                    {
                        Vector3? pickupSpawnPosition = null;

                        if (inventory.TryGetComponent(out CharacterMaster characterMaster))
                        {
                            CharacterBody body = characterMaster.GetBody();
                            if (body)
                            {
                                pickupSpawnPosition = body.footPosition;
                            }
                        }

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

                inventory.SetEquipmentIndexForSlot(pickup.equipmentIndex, currentEquipmentSlot);
                return true;
            }
            else
            {
                Log.Error($"Invalid pickup type: {pickup.internalName}");
                return false;
            }
        }

        public static bool TryRemove(this Inventory inventory, PickupIndex pickupIndex)
        {
            return TryRemove(inventory, PickupCatalog.GetPickupDef(pickupIndex));
        }

        public static bool TryRemove(this Inventory inventory, PickupDef pickupDef)
        {
            if (!inventory || pickupDef == null)
                return false;

            if (pickupDef.itemIndex != ItemIndex.None)
            {
                inventory.RemoveItem(pickupDef.itemIndex);
                return true;
            }
            else if (pickupDef.equipmentIndex != EquipmentIndex.None)
            {
                int equipmentSlotCount = inventory.GetEquipmentSlotCount();
                for (uint i = 0; i < equipmentSlotCount; i++)
                {
                    if (inventory.GetEquipment(i).equipmentIndex == pickupDef.equipmentIndex)
                    {
                        inventory.SetEquipmentIndexForSlot(EquipmentIndex.None, i);
                        return true;
                    }
                }

                return false;
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
