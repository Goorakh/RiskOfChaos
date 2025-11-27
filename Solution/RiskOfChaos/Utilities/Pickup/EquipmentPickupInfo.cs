using RoR2;

namespace RiskOfChaos.Utilities.Pickup
{
    public sealed record class EquipmentPickupInfo : PickupInfo
    {
        public readonly EquipmentIndex EquipmentIndex;
        public readonly uint EquipmentSlotIndex;
        public readonly uint EquipmentSetIndex;

        public EquipmentPickupInfo(Inventory inventory, EquipmentIndex equipmentIndex, uint equipmentSlotIndex, uint equipmentSetIndex) : base(inventory, PickupCatalog.FindPickupIndex(equipmentIndex))
        {
            EquipmentIndex = equipmentIndex;
            EquipmentSlotIndex = equipmentSlotIndex;
            EquipmentSetIndex = equipmentSetIndex;
        }

        public override void RemoveFromInventory()
        {
            Inventory.SetEquipmentIndexForSlot(EquipmentIndex.None, EquipmentSlotIndex, EquipmentSetIndex);
        }
    }
}
