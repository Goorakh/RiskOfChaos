using RiskOfChaos.EffectDefinitions;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;

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

            int slotCount = inventory.GetEquipmentSlotCount();
            for (slotIndex = 0; slotIndex < slotCount; slotIndex++)
            {
                if (inventory.GetEquipment(slotIndex).equipmentIndex == EquipmentIndex.None)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool TryGrant(this Inventory inventory, PickupDef pickup)
        {
            if (!inventory)
                return false;

            if (pickup.itemIndex != ItemIndex.None)
            {
                inventory.GiveItem(pickup.itemIndex, 1);
                return true;
            }
            else if (pickup.equipmentIndex != EquipmentIndex.None)
            {
                if (inventory.TryGetFreeEquipmentSlot(out uint freeSlot))
                {
                    inventory.SetEquipmentIndexForSlot(pickup.equipmentIndex, freeSlot);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                Log.Warning($"unhandled pickup index {pickup.pickupIndex}");
                return false;
            }
        }
    }
}
