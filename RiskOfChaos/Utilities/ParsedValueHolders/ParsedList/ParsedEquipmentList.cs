using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System;
using System.Collections.Generic;

namespace RiskOfChaos.Utilities.ParsedValueHolders.ParsedList
{
    public class ParsedEquipmentList : GenericParsedList<EquipmentIndex>
    {
        static readonly char[] _equipmentNameFilterChars = new char[] { ',', ' ' };

        public ParsedEquipmentList(IComparer<EquipmentIndex> comparer) : base(comparer)
        {
            setupParseReadyListener();
        }

        public ParsedEquipmentList() : base()
        {
            setupParseReadyListener();
        }

        void setupParseReadyListener()
        {
            if (!EquipmentCatalog.availability.available)
            {
                ParseReady = false;
                EquipmentCatalog.availability.onAvailable += () =>
                {
                    ParseReady = true;
                };
            }
        }

        protected override IEnumerable<string> splitInput(string input)
        {
            return input.Split(',');
        }

        protected override EquipmentIndex parseValue(string str)
        {
            if (TryParseEquipmentIndex(str, out EquipmentIndex index))
            {
                return index;
            }
            else
            {
                throw new ParseException($"Unable to find matching EquipmentDef");
            }
        }

        public static bool TryParseEquipmentIndex(string str, out EquipmentIndex result)
        {
            result = EquipmentCatalog.FindEquipmentIndex(str);
            if (result != EquipmentIndex.None)
                return true;

            bool compareName(string equipmentName)
            {
                if (string.Equals(equipmentName.FilterChars(_equipmentNameFilterChars), str, StringComparison.OrdinalIgnoreCase))
                    return true;

                return false;
            }

            foreach (EquipmentIndex equipmentIndex in EquipmentCatalog.allEquipment)
            {
                EquipmentDef equipmentDef = EquipmentCatalog.GetEquipmentDef(equipmentIndex);
                if (compareName(equipmentDef.name)
                    || compareName(equipmentDef.nameToken)
                    || compareName(Language.GetString(equipmentDef.nameToken, "en")))
                {
                    result = equipmentIndex;
                    return true;
                }
            }

            return false;
        }
    }
}
