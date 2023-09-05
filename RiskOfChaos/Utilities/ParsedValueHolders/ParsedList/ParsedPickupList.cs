using RoR2;
using System.Collections.Generic;

namespace RiskOfChaos.Utilities.ParsedValueHolders.ParsedList
{
    public class ParsedPickupList : GenericParsedList<PickupIndex>
    {
        public ParsedPickupList(IComparer<PickupIndex> comparer) : base(comparer)
        {
            setupParseReadyListener();
        }

        public ParsedPickupList() : base()
        {
            setupParseReadyListener();
        }

        void setupParseReadyListener()
        {
            if (!AdditionalResourceAvailability.PickupCatalog.available)
            {
                ParseReady = false;
                AdditionalResourceAvailability.PickupCatalog.onAvailable += () =>
                {
                    ParseReady = true;
                };
            }
        }

        protected override IEnumerable<string> splitInput(string input)
        {
            return input.Split(',');
        }

        protected override PickupIndex parseValue(string str)
        {
            if (TryParsePickupIndex(str, out PickupIndex pickupIndex))
            {
                return pickupIndex;
            }
            else
            {
                throw new ParseException($"Unable to find matching PickupDef");
            }
        }

        public static bool TryParsePickupIndex(string str, out PickupIndex pickupIndex)
        {
            pickupIndex = PickupCatalog.FindPickupIndex(str);
            if (pickupIndex.isValid)
            {
                return true;
            }
            else if (ParsedItemList.TryParseItemIndex(str, out ItemIndex itemIndex))
            {
                pickupIndex = PickupCatalog.FindPickupIndex(itemIndex);
                return pickupIndex.isValid;
            }
            else if (ParsedEquipmentList.TryParseEquipmentIndex(str, out EquipmentIndex equipmentIndex))
            {
                pickupIndex = PickupCatalog.FindPickupIndex(equipmentIndex);
                return pickupIndex.isValid;
            }
            else
            {
                return false;
            }
        }
    }
}
