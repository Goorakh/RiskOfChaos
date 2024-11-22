using RoR2;

namespace RiskOfChaos.Collections.CatalogIndex
{
    public sealed class BodyIndexCollection : CatalogIndexCollection<BodyIndex>
    {
        public BodyIndexCollection(string[] names) : base(names)
        {
            BodyCatalog.availability.CallWhenAvailable(initialize);
        }

        protected override BodyIndex findByName(string name)
        {
            return BodyCatalog.FindBodyIndex(name);
        }

        protected override bool isValid(BodyIndex value)
        {
            return value != BodyIndex.None;
        }
    }
}
