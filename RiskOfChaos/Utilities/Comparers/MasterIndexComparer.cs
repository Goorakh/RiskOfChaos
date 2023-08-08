using RoR2;
using System.Collections.Generic;

namespace RiskOfChaos.Utilities.Comparers
{
    public sealed class MasterIndexComparer : IComparer<MasterCatalog.MasterIndex>
    {
        public static readonly MasterIndexComparer Instance = new MasterIndexComparer();

        MasterIndexComparer()
        {
        }

        public int Compare(MasterCatalog.MasterIndex x, MasterCatalog.MasterIndex y)
        {
            return ((int)x).CompareTo((int)y);
        }
    }
}
