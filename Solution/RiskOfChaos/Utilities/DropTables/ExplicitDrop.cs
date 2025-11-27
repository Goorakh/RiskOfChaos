using RoR2;
using RoR2.ExpansionManagement;

namespace RiskOfChaos.Utilities.DropTables
{
    public readonly record struct ExplicitDrop(UniquePickup Pickup, DropType DropType, ExpansionIndex RequiredExpansion)
    {
        public ExplicitDrop(ItemIndex ItemIndex, DropType DropType, ExpansionIndex RequiredExpansion) : this(new UniquePickup(PickupCatalog.FindPickupIndex(ItemIndex)), DropType, RequiredExpansion)
        {
        }

        public ExplicitDrop(EquipmentIndex EquipmentIndex, DropType DropType, ExpansionIndex RequiredExpansion) : this(new UniquePickup(PickupCatalog.FindPickupIndex(EquipmentIndex)), DropType, RequiredExpansion)
        {
        }
    }
}
