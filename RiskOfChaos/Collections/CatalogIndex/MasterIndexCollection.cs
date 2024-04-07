using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Comparers;
using RoR2;

namespace RiskOfChaos.Collections.CatalogIndex
{
    public sealed class MasterIndexCollection : CatalogIndexCollection<MasterCatalog.MasterIndex>
    {
        public MasterIndexCollection(params string[] names) : base(names)
        {
            Comparer = MasterIndexComparer.Instance;
            AdditionalResourceAvailability.MasterCatalog.CallWhenAvailable(initialize);
        }

        protected override bool isValid(MasterCatalog.MasterIndex value)
        {
            return value.isValid;
        }

        protected override MasterCatalog.MasterIndex findByName(string name)
        {
            return MasterCatalog.FindMasterIndex(name);
        }
    }
}
