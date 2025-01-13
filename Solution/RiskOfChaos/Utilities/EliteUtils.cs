using RoR2;
using System;
using System.Collections.Generic;

namespace RiskOfChaos.Utilities
{
    public static class EliteUtils
    {
        static EliteIndex[] _validEliteIndices = [];
        static EquipmentIndex[] _validEliteEquipmentIndices = [];

        static EquipmentIndex[] _allEliteEquipmentIndices = [];

        static EliteIndex[] _runAvailableEliteIndices = [];

        public static bool HasAnyRunAvailableElites => _runAvailableEliteIndices.Length > 0;

        [SystemInitializer(typeof(EquipmentCatalog), typeof(EliteCatalog))]
        static void Init()
        {
            int eliteCount = EliteCatalog.eliteList.Count;
            HashSet<EliteIndex> validEliteIndices = new HashSet<EliteIndex>(eliteCount);
            HashSet<EquipmentIndex> validEliteEquipmentIndices = new HashSet<EquipmentIndex>(eliteCount);
            HashSet<EquipmentIndex> allEliteEquipmentIndices = new HashSet<EquipmentIndex>(eliteCount);

            foreach (EliteIndex eliteIndex in EliteCatalog.eliteList)
            {
                EliteDef eliteDef = EliteCatalog.GetEliteDef(eliteIndex);
                if (!eliteDef)
                    continue;

                EquipmentDef equipmentDef = eliteDef.eliteEquipmentDef;
                if (!equipmentDef || equipmentDef.equipmentIndex == EquipmentIndex.None)
                {
                    continue;
                }

                allEliteEquipmentIndices.Add(equipmentDef.equipmentIndex);

                if (string.IsNullOrWhiteSpace(eliteDef.modifierToken) || Language.IsTokenInvalid(eliteDef.modifierToken))
                    continue;

                if (eliteDef.name.EndsWith("Honor", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!equipmentDef.pickupModelPrefab || string.Equals(equipmentDef.pickupModelPrefab.name, "NullModel", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (validEliteEquipmentIndices.Add(equipmentDef.equipmentIndex))
                {
                    validEliteIndices.Add(eliteIndex);
                }
            }

            _validEliteIndices = [.. validEliteIndices];
            Array.Sort(_validEliteIndices);

            _validEliteEquipmentIndices = [.. validEliteEquipmentIndices];
            Array.Sort(_validEliteEquipmentIndices);

            _allEliteEquipmentIndices = [.. allEliteEquipmentIndices];
            Array.Sort(_allEliteEquipmentIndices);

            if (_validEliteIndices.Length > 0)
            {
                Run.onRunStartGlobal += onRunStartGlobal;
                Run.onRunDestroyGlobal += onRunDestroyGlobal;
            }

#if DEBUG
            foreach (EliteIndex eliteIndex in EliteCatalog.eliteList)
            {
                EliteDef eliteDef = EliteCatalog.GetEliteDef(eliteIndex);
                if (!eliteDef)
                    continue;

                EquipmentDef eliteEquipmentDef = eliteDef.eliteEquipmentDef;
                string equipmentName = "null";
                if (eliteEquipmentDef)
                {
                    equipmentName = Language.GetString(eliteEquipmentDef.nameToken);
                }

                if (IsAvailable(eliteIndex))
                {
                    Log.Debug($"Included elite equipment {eliteEquipmentDef} ({equipmentName}) ({eliteDef})");
                }
                else
                {
                    Log.Debug($"Excluded elite equipment {eliteEquipmentDef} ({equipmentName}) ({eliteDef})");
                }
            }
#endif
        }

        static void onRunStartGlobal(Run run)
        {
            List<EliteIndex> runAvailableEliteIndices = new List<EliteIndex>(_validEliteIndices.Length);

            foreach (EliteIndex eliteIndex in _validEliteIndices)
            {
                EliteDef eliteDef = EliteCatalog.GetEliteDef(eliteIndex);
                if (!eliteDef || !eliteDef.IsAvailable())
                    continue;

                runAvailableEliteIndices.Add(eliteIndex);
            }

            _runAvailableEliteIndices = [.. runAvailableEliteIndices];
        }

        static void onRunDestroyGlobal(Run run)
        {
            _runAvailableEliteIndices = [];
        }

        public static bool IsAvailable(EliteIndex eliteIndex)
        {
            return Array.BinarySearch(_validEliteIndices, eliteIndex) >= 0;
        }

        public static bool IsRunAvailable(EliteIndex eliteIndex)
        {
            return Array.BinarySearch(_runAvailableEliteIndices, eliteIndex) >= 0;
        }

        public static IReadOnlyList<EliteIndex> GetRunAvailableElites(bool ignoreEliteTierAvailability)
        {
            return ignoreEliteTierAvailability ? Array.AsReadOnly(_runAvailableEliteIndices) : GetAllCombatDirectorElites();
        }

        public static IReadOnlyList<EliteIndex> GetAllCombatDirectorElites()
        {
            List<EliteIndex> availableEliteIndices = new List<EliteIndex>(_runAvailableEliteIndices.Length);

            foreach (CombatDirector.EliteTierDef eliteTier in CombatDirector.eliteTiers)
            {
                if (eliteTier.CanSelect(SpawnCard.EliteRules.Default))
                {
                    foreach (EliteDef eliteDef in eliteTier.eliteTypes)
                    {
                        if (eliteDef && IsRunAvailable(eliteDef.eliteIndex))
                        {
                            if (!availableEliteIndices.Contains(eliteDef.eliteIndex))
                            {
                                availableEliteIndices.Add(eliteDef.eliteIndex);
                            }
                        }
                    }
                }
            }

            return availableEliteIndices.AsReadOnly();
        }

        public static bool IsEliteEquipment(EquipmentIndex equipmentIndex)
        {
            return Array.BinarySearch(_allEliteEquipmentIndices, equipmentIndex) >= 0;
        }
    }
}
