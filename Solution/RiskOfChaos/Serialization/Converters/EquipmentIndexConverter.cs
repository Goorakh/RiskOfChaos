using RoR2;

namespace RiskOfChaos.Serialization.Converters
{
    public sealed class EquipmentIndexConverter : CatalogValueConverter<EquipmentIndex>
    {
        public EquipmentIndexConverter() : base(EquipmentIndex.None)
        {
        }

        protected override EquipmentIndex findFromCatalog(string catalogName)
        {
            return EquipmentCatalog.FindEquipmentIndex(catalogName);
        }

        protected override string getCatalogName(EquipmentIndex value)
        {
            EquipmentDef equipmentDef = EquipmentCatalog.GetEquipmentDef(value);
            return equipmentDef ? equipmentDef.name : string.Empty;
        }
    }
}
