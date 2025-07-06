using RoR2;
using RoR2.ExpansionManagement;

namespace RiskOfChaos.Utilities.DropTables
{
    public readonly record struct ExplicitDrop(PickupIndex PickupIndex, DropType DropType, ExpansionIndex RequiredExpansion)
    {
        public ExplicitDrop(ItemIndex ItemIndex, DropType DropType, ExpansionIndex RequiredExpansion) : this(PickupCatalog.FindPickupIndex(ItemIndex), DropType, RequiredExpansion)
        {
        }

        public ExplicitDrop(EquipmentIndex EquipmentIndex, DropType DropType, ExpansionIndex RequiredExpansion) : this(PickupCatalog.FindPickupIndex(EquipmentIndex), DropType, RequiredExpansion)
        {
        }
    }
}
