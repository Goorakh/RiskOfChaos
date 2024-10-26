using RoR2;

namespace RiskOfChaos.Serialization.Converters
{
    public sealed class PickupIndexConverter : CatalogValueConverter<PickupIndex>
    {
        public PickupIndexConverter() : base(PickupIndex.none)
        {
        }

        protected override PickupIndex findFromCatalog(string catalogName)
        {
            return PickupCatalog.FindPickupIndex(catalogName);
        }

        protected override string getCatalogName(PickupIndex value)
        {
            PickupDef pickupDef = PickupCatalog.GetPickupDef(value);
            return pickupDef != null ? pickupDef.internalName : string.Empty;
        }
    }
}
