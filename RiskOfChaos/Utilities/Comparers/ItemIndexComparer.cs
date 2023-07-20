using RoR2;
using System.Collections;
using System.Collections.Generic;

namespace RiskOfChaos.Utilities.Comparers
{
    public sealed class ItemIndexComparer : IComparer<ItemIndex>, IComparer
    {
        public static readonly ItemIndexComparer Instance = new ItemIndexComparer();

        public int Compare(ItemIndex x, ItemIndex y)
        {
            return ((int)x).CompareTo((int)y);
        }

        public int Compare(object x, object y)
        {
            if (x is ItemIndex itemX && y is ItemIndex itemY)
            {
                return Compare(itemX, itemY);
            }

            return Comparer.Default.Compare(x, y);
        }
    }
}
