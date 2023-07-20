using HG;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System;
using System.Collections.Generic;

namespace RiskOfChaos.Utilities.ParsedValueHolders.ParsedList
{
    public class ParsedItemList : GenericParsedList<ItemIndex>
    {
        static readonly char[] _itemNameFilterChars = new char[] { ',' };

        public ParsedItemList(IComparer<ItemIndex> comparer) : base(comparer)
        {
        }

        public ParsedItemList() : base()
        {
        }

        protected override IEnumerable<string> splitInput(string input)
        {
            return input.Split(',');
        }

        protected override bool tryParse(string input, out ItemIndex value)
        {
            value = ItemCatalog.FindItemIndex(input);
            if (value != ItemIndex.None)
                return true;

            string trimmedInput = input.Trim();
            value = ItemCatalog.FindItemIndex(trimmedInput);
            if (value != ItemIndex.None)
                return true;

            bool compareName(string itemName)
            {
                if (string.Equals(itemName, input, StringComparison.OrdinalIgnoreCase))
                    return true;

                if (string.Equals(itemName, trimmedInput, StringComparison.OrdinalIgnoreCase))
                    return true;

                return false;
            }

            ReadOnlyArray<ItemDef> allItems = ItemCatalog.allItemDefs;
            for (int i = 0; i < allItems.Length; i++)
            {
                ItemDef item = allItems[i];
                if (compareName(item.name)
                    || compareName(item.nameToken)
                    || compareName(Language.GetString(item.nameToken, "en").FilterChars(_itemNameFilterChars)))
                {
                    value = item.itemIndex;
                    return true;
                }
            }

            value = default;
            return false;
        }
    }
}
