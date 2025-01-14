﻿using RoR2;
using System;
using System.Collections.Generic;

namespace RiskOfChaos.Utilities
{
    public static class EliteUtils
    {
        static EliteIndex[] _baseEliteIndices = [];
        static EquipmentIndex[] _baseEliteEquipmentIndices = [];

        static EliteIndex[] _runAvailableEliteIndices = [];

        public static bool HasAnyRunAvailableElites => _runAvailableEliteIndices.Length > 0;

        [SystemInitializer(typeof(EquipmentCatalog), typeof(EliteCatalog))]
        static void Init()
        {
            List<EliteIndex> validEliteIndices = new List<EliteIndex>(EliteCatalog.eliteList.Count);
            List<EquipmentIndex> validEliteEquipmentIndices = new List<EquipmentIndex>(EliteCatalog.eliteList.Count);

            foreach (EliteIndex eliteIndex in EliteCatalog.eliteList)
            {
                EliteDef eliteDef = EliteCatalog.GetEliteDef(eliteIndex);
                if (!eliteDef)
                    continue;

                if (eliteDef.name.EndsWith("Honor", StringComparison.OrdinalIgnoreCase))
                    continue;

                EquipmentDef equipmentDef = eliteDef.eliteEquipmentDef;
                if (!equipmentDef ||
                    !equipmentDef.pickupModelPrefab ||
                    string.Equals(equipmentDef.pickupModelPrefab.name, "NullModel", StringComparison.OrdinalIgnoreCase) ||
                    equipmentDef.dropOnDeathChance <= 0f)
                {
                    continue;
                }

                if (!validEliteIndices.Contains(eliteIndex))
                {
                    validEliteIndices.Add(eliteIndex);
                }

                if (!validEliteEquipmentIndices.Contains(equipmentDef.equipmentIndex))
                {
                    validEliteEquipmentIndices.Add(equipmentDef.equipmentIndex);
                }
            }

            validEliteIndices.Sort();
            _baseEliteIndices = [.. validEliteIndices];

            validEliteEquipmentIndices.Sort();
            _baseEliteEquipmentIndices = [.. validEliteEquipmentIndices];

            Run.onRunStartGlobal += run =>
            {
                List<EliteIndex> runAvailableEliteIndices = new List<EliteIndex>(_baseEliteIndices.Length);

                foreach (EliteIndex eliteIndex in _baseEliteIndices)
                {
                    EliteDef eliteDef = EliteCatalog.GetEliteDef(eliteIndex);
                    if (!eliteDef || !eliteDef.IsAvailable())
                        continue;

                    runAvailableEliteIndices.Add(eliteIndex);
                }

                _runAvailableEliteIndices = [.. runAvailableEliteIndices];
            };

            Run.onRunDestroyGlobal += _ =>
            {
                _runAvailableEliteIndices = [];
            };

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

        public static bool IsAvailable(EliteIndex eliteIndex)
        {
            return Array.BinarySearch(_baseEliteIndices, eliteIndex) >= 0;
        }

        public static bool IsRunAvailable(EliteIndex eliteIndex)
        {
            return Array.BinarySearch(_runAvailableEliteIndices, eliteIndex) >= 0;
        }

        public static EliteIndex[] GetRunAvailableElites(bool ignoreEliteTierAvailability)
        {
            List<EliteIndex> availableEliteIndices = new List<EliteIndex>(_runAvailableEliteIndices.Length);

            foreach (CombatDirector.EliteTierDef eliteTier in CombatDirector.eliteTiers)
            {
                if (ignoreEliteTierAvailability || eliteTier.CanSelect(SpawnCard.EliteRules.Default))
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

            return [.. availableEliteIndices];
        }

        public static bool IsEliteEquipment(EquipmentIndex equipmentIndex)
        {
            return Array.BinarySearch(_baseEliteEquipmentIndices, equipmentIndex) >= 0;
        }
    }
}
