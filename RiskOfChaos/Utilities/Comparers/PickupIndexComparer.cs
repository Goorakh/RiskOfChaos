using RoR2;
using System;
using System.Collections.Generic;
using System.Text;

namespace RiskOfChaos.Utilities.Comparers
{
    public sealed class PickupIndexComparer : IComparer<PickupIndex>
    {
        public static readonly PickupIndexComparer Instance = new PickupIndexComparer();

        public int Compare(PickupIndex x, PickupIndex y)
        {
            return x.value.CompareTo(y.value);
        }
    }
}
