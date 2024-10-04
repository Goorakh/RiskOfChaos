using RoR2;

namespace RiskOfChaos.Serialization.Converters
{
    public class BuffIndexConverter : CatalogValueConverter<BuffIndex>
    {
        public BuffIndexConverter() : base(BuffIndex.None)
        {
        }

        protected override BuffIndex findFromCatalog(string catalogName)
        {
            return BuffCatalog.FindBuffIndex(catalogName);
        }

        protected override string getCatalogName(BuffIndex value)
        {
            BuffDef buffDef = BuffCatalog.GetBuffDef(value);
            return buffDef ? buffDef.name : string.Empty;
        }
    }
}
