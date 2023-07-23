using HG;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System;
using System.Collections.Generic;

namespace RiskOfChaos.Utilities.ParsedValueHolders.ParsedList
{
    public class ParsedItemList : GenericParsedList<ItemIndex>
    {
        static readonly char[] _itemNameFilterChars = new char[] { ',', ' ' };

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

        protected override ItemIndex parseValue(string str)
        {
            ItemIndex result = ItemCatalog.FindItemIndex(str);
            if (result != ItemIndex.None)
                return result;

            bool compareName(string itemName)
            {
                if (string.Equals(itemName.FilterChars(_itemNameFilterChars), str, StringComparison.OrdinalIgnoreCase))
                    return true;

                return false;
            }

            ReadOnlyArray<ItemDef> allItems = ItemCatalog.allItemDefs;
            for (int i = 0; i < allItems.Length; i++)
            {
                ItemDef item = allItems[i];
                if (compareName(item.name)
                    || compareName(item.nameToken)
                    || compareName(Language.GetString(item.nameToken, "en")))
                {
                    return item.itemIndex;
                }
            }

            throw new ParseException($"Unable to find matching ItemDef");
        }
    }
}
