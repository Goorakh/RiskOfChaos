using RoR2;

namespace RiskOfChaos.Serialization.Converters
{
    public sealed class ItemIndexConverter : CatalogValueConverter<ItemIndex>
    {
        public ItemIndexConverter() : base(ItemIndex.None)
        {
        }

        protected override ItemIndex findFromCatalog(string catalogName)
        {
            return ItemCatalog.FindItemIndex(catalogName);
        }

        protected override string getCatalogName(ItemIndex value)
        {
            ItemDef itemDef = ItemCatalog.GetItemDef(value);
            return itemDef ? itemDef.name : string.Empty;
        }
    }
}
