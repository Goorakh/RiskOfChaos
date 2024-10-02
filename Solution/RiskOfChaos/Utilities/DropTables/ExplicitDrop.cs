using RoR2;
using RoR2.ExpansionManagement;

namespace RiskOfChaos.Utilities.DropTables
{
    public readonly record struct ExplicitDrop(PickupIndex PickupIndex, DropType DropType, ExpansionDef RequiredExpansion)
    {
        public ExplicitDrop(ItemIndex ItemIndex, DropType DropType, ExpansionDef RequiredExpansion) : this(PickupCatalog.FindPickupIndex(ItemIndex), DropType, RequiredExpansion)
        {
        }

        public ExplicitDrop(EquipmentIndex EquipmentIndex, DropType DropType, ExpansionDef RequiredExpansion) : this(PickupCatalog.FindPickupIndex(EquipmentIndex), DropType, RequiredExpansion)
        {
        }
    }
}
