using RoR2;

namespace RiskOfChaos.Utilities.Extensions
{
    public static class InventoryExtensions
    {
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

        public static PickupTryGrantResult TryGrant(this Inventory inventory, PickupDef pickup, bool replaceExisting)
        {
            if (!inventory)
                return PickupTryGrantResult.Failed;

            if (pickup.itemIndex != ItemIndex.None)
            {
                inventory.GiveItem(pickup.itemIndex, 1);
                return PickupTryGrantResult.CompleteSuccess;
            }
            else if (pickup.equipmentIndex != EquipmentIndex.None)
            {
                if (inventory.TryGetFreeEquipmentSlot(out uint freeSlot))
                {
                    inventory.SetEquipmentIndexForSlot(pickup.equipmentIndex, freeSlot);
                    return PickupTryGrantResult.CompleteSuccess;
                }
                else if (replaceExisting)
                {
                    EquipmentIndex previousEquipmentIndex = inventory.currentEquipmentIndex;
                    inventory.SetEquipmentIndex(pickup.equipmentIndex);

                    if (previousEquipmentIndex != EquipmentIndex.None)
                    {
                        return PickupTryGrantResult.PartialSuccess(PickupCatalog.FindPickupIndex(previousEquipmentIndex));
                    }
                    else
                    {
                        Log.Warning($"{nameof(TryGetFreeEquipmentSlot)} failed, but current equipment index is None, this should never be able to happen");
                        return PickupTryGrantResult.CompleteSuccess; // Success because the current equipment was set
                    }
                }
                else
                {
                    return PickupTryGrantResult.Failed;
                }
            }
            else
            {
                Log.Warning($"unhandled pickup index {pickup.pickupIndex}");
                return PickupTryGrantResult.Failed;
            }
        }

        public static void TryRemove(this Inventory inventory, PickupDef pickupDef)
        {
            if (!inventory || pickupDef == null)
                return;

            if (pickupDef.itemIndex != ItemIndex.None)
            {
                inventory.RemoveItem(pickupDef.itemIndex);
            }
            else if (pickupDef.equipmentIndex != EquipmentIndex.None)
            {
                int equipmentSlotCount = inventory.GetEquipmentSlotCount();
                for (uint i = 0; i < equipmentSlotCount; i++)
                {
                    if (inventory.GetEquipment(i).equipmentIndex == pickupDef.equipmentIndex)
                    {
                        inventory.SetEquipmentIndexForSlot(EquipmentIndex.None, i);
                        break;
                    }
                }
            }
            else
            {
                Log.Warning($"Unhandled pickup index {pickupDef.pickupIndex}");
            }
        }
    }
}
