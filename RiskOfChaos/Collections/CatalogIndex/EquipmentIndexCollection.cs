using RoR2;

namespace RiskOfChaos.Collections.CatalogIndex
{
    public sealed class EquipmentIndexCollection : CatalogIndexCollection<EquipmentIndex>
    {
        public EquipmentIndexCollection(params string[] names) : base(names)
        {
            EquipmentCatalog.availability.CallWhenAvailable(initialize);
        }

        protected override bool isValid(EquipmentIndex value)
        {
            return value != EquipmentIndex.None;
        }

        protected override EquipmentIndex findByName(string name)
        {
            return EquipmentCatalog.FindEquipmentIndex(name);
        }
    }
}
