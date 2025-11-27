using HG;
using RiskOfChaos.Utilities.Extensions;
using RiskOfChaos.Utilities.ParsedValueHolders;
using RoR2;
using System;
using System.Collections.Generic;

namespace RiskOfChaos.Collections.ParsedValue
{
    public sealed class ParsedItemList : GenericParsedList<ItemIndex>
    {
        static readonly char[] _itemNameFilterChars = [',', ' '];

        public ParsedItemList(IComparer<ItemIndex> comparer) : base(comparer)
        {
            setupParseReadyListener();
        }

        public ParsedItemList() : base()
        {
            setupParseReadyListener();
        }

        void setupParseReadyListener()
        {
            if (!ItemCatalog.availability.available)
            {
                ParseReady = false;
                ItemCatalog.availability.CallWhenAvailable(() => ParseReady = true);
            }
        }

        protected override IEnumerable<string> splitInput(string input)
        {
            return input.Split(',');
        }

        protected override ItemIndex parseValue(string str)
        {
            if (TryParseItemIndex(str, out ItemIndex itemIndex))
            {
                return itemIndex;
            }
            else
            {
                throw new ParseException($"Unable to find matching ItemDef");
            }
        }

        public static bool TryParseItemIndex(string str, out ItemIndex result)
        {
            result = ItemCatalog.FindItemIndex(str);
            if (result != ItemIndex.None)
                return true;

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
                    result = item.itemIndex;
                    return true;
                }
            }

            return false;
        }
    }
}
