﻿using RiskOfChaos.Utilities;
using RoR2;

namespace RiskOfChaos.Collections.CatalogIndex
{
    public sealed class BuffIndexCollection : CatalogIndexCollection<BuffIndex>
    {
        public BuffIndexCollection(params string[] names) : base(names)
        {
            AdditionalResourceAvailability.BuffCatalog.CallWhenAvailable(initialize);
        }

        protected override bool isValid(BuffIndex value)
        {
            return value != BuffIndex.None;
        }

        protected override BuffIndex findByName(string name)
        {
            return BuffCatalog.FindBuffIndex(name);
        }
    }
}
