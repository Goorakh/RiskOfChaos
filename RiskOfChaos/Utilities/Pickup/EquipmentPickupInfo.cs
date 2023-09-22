using RoR2;

namespace RiskOfChaos.Utilities.Pickup
{
    public sealed record class EquipmentPickupInfo : PickupInfo
    {
        public readonly EquipmentIndex EquipmentIndex;
        public readonly uint EquipmentSlotIndex;

        public EquipmentPickupInfo(Inventory inventory, EquipmentIndex equipmentIndex, uint equipmentSlotIndex) : base(inventory, PickupCatalog.FindPickupIndex(equipmentIndex))
        {
            EquipmentSlotIndex = equipmentSlotIndex;
            EquipmentIndex = equipmentIndex;
        }

        public override void RemoveFromInventory()
        {
            Inventory.SetEquipmentIndexForSlot(EquipmentIndex.None, EquipmentSlotIndex);
        }
    }
}
