using RiskOfChaos.Utilities.Extensions;
using RiskOfChaos.Utilities.ParsedValueHolders;
using RoR2;
using System;
using System.Collections.Generic;

namespace RiskOfChaos.Collections.ParsedValue
{
    public sealed class ParsedBodyList : GenericParsedList<BodyIndex>
    {
        static readonly char[] _itemNameFilterChars = [',', ' '];

        public ParsedBodyList(IComparer<BodyIndex> comparer) : base(comparer)
        {
            setupParseReadyListener();
        }

        public ParsedBodyList() : this(null)
        {
        }

        void setupParseReadyListener()
        {
            if (!BodyCatalog.availability.available)
            {
                ParseReady = false;
                BodyCatalog.availability.CallWhenAvailable(() => ParseReady = true);
            }
        }

        protected override IEnumerable<string> splitInput(string input)
        {
            return input.Split(',');
        }

        protected override BodyIndex parseValue(string str)
        {
            if (TryParseBodyIndex(str, out BodyIndex bodyIndex))
            {
                return bodyIndex;
            }
            else
            {
                throw new ParseException($"Unable to find matching BodyDef");
            }
        }

        public static bool TryParseBodyIndex(string str, out BodyIndex bodyIndex)
        {
            bool matchesName(string bodyName)
            {
                return !string.IsNullOrWhiteSpace(bodyName) && string.Equals(bodyName.FilterChars(_itemNameFilterChars), str, StringComparison.OrdinalIgnoreCase);
            }

            if (!string.IsNullOrEmpty(str))
            {
                foreach (CharacterBody bodyPrefab in BodyCatalog.allBodyPrefabBodyBodyComponents)
                {
                    if (matchesName(bodyPrefab.baseNameToken) ||
                        matchesName(Language.english.GetLocalizedStringByToken(bodyPrefab.baseNameToken)) ||
                        matchesName(bodyPrefab.name))
                    {
                        bodyIndex = bodyPrefab.bodyIndex;
                        return true;
                    }
                }
            }

            bodyIndex = BodyIndex.None;
            return false;
        }
    }
}
