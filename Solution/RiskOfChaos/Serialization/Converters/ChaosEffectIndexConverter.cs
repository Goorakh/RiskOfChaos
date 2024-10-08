using RiskOfChaos.EffectHandling;

namespace RiskOfChaos.Serialization.Converters
{
    public sealed class ChaosEffectIndexConverter : CatalogValueConverter<ChaosEffectIndex>
    {
        public ChaosEffectIndexConverter() : base(ChaosEffectIndex.Invalid)
        {
        }

        protected override ChaosEffectIndex findFromCatalog(string catalogName)
        {
            return ChaosEffectCatalog.FindEffectIndex(catalogName);
        }

        protected override string getCatalogName(ChaosEffectIndex value)
        {
            ChaosEffectInfo effectInfo = ChaosEffectCatalog.GetEffectInfo(value);
            return effectInfo != null ? effectInfo.Identifier : string.Empty;
        }
    }
}
