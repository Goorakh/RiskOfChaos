using RoR2;

namespace RiskOfChaos.Serialization.Converters
{
    public sealed class BodyIndexConverter : CatalogValueConverter<BodyIndex>
    {
        public BodyIndexConverter() : base(BodyIndex.None)
        {
        }

        protected override BodyIndex findFromCatalog(string catalogName)
        {
            return BodyCatalog.FindBodyIndex(catalogName);
        }

        protected override string getCatalogName(BodyIndex value)
        {
            return BodyCatalog.GetBodyName(value);
        }
    }
}
