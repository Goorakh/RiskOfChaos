using RoR2;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace RiskOfChaos.Utilities
{
    public static class EliteUtils
    {
        static EliteUtils()
        {
            baseEliteEquipments = [];
            runAvailableEliteEquipments = [];
        }

        static EquipmentIndex[] _baseEliteEquipments;
        static EquipmentIndex[] baseEliteEquipments
        {
            get
            {
                return _baseEliteEquipments;
            }
            set
            {
                value ??= [];

                if (_baseEliteEquipments == null || !_baseEliteEquipments.SequenceEqual(value))
                {
                    _baseEliteEquipments = value;
                    AllEliteEquipments = Array.AsReadOnly(value);
                }
            }
        }

        public static ReadOnlyCollection<EquipmentIndex> AllEliteEquipments { get; private set; }

        [SystemInitializer(typeof(EquipmentCatalog), typeof(EliteCatalog))]
        static void InitBaseEquipments()
        {
            baseEliteEquipments = EliteCatalog.eliteList
                                              .Select(i => EliteCatalog.GetEliteDef(i).eliteEquipmentDef)
                                              .Where(e => e.pickupModelPrefab && e.pickupModelPrefab.name != "NullModel" && e.dropOnDeathChance > 0f)
                                              .Select(e => e.equipmentIndex)
                                              .Distinct()
                                              .OrderBy(i => i)
                                              .ToArray();

#if DEBUG
            foreach (EquipmentDef eliteEquipmentDef in EliteCatalog.eliteList.Select(i => EliteCatalog.GetEliteDef(i).eliteEquipmentDef).Distinct())
            {
                EliteDef eliteDef = EliteCatalog.GetEliteDef(EliteCatalog.eliteList.FirstOrDefault(i => EliteCatalog.GetEliteDef(i).eliteEquipmentDef == eliteEquipmentDef));

                if (Array.BinarySearch(baseEliteEquipments, eliteEquipmentDef.equipmentIndex) < 0)
                {
                    Log.Debug($"Excluded elite equipment {eliteEquipmentDef} ({Language.GetString(eliteEquipmentDef.nameToken)}) ({eliteDef})");
                }
                else
                {
                    Log.Debug($"Included elite equipment {eliteEquipmentDef} ({Language.GetString(eliteEquipmentDef.nameToken)}) ({eliteDef})");
                }
            }
#endif
        }

        static EquipmentIndex[] _runAvailableEliteEquipments;
        static EquipmentIndex[] runAvailableEliteEquipments
        {
            get
            {
                return _runAvailableEliteEquipments;
            }
            set
            {
                value ??= [];

                if (_runAvailableEliteEquipments == null || !_runAvailableEliteEquipments.SequenceEqual(value))
                {
                    _runAvailableEliteEquipments = value;
                    RunAvailableEliteEquipments = Array.AsReadOnly(value);
                }
            }
        }

        public static ReadOnlyCollection<EquipmentIndex> RunAvailableEliteEquipments { get; private set; }

        [SystemInitializer]
        static void Init()
        {
            Run.onRunStartGlobal += run =>
            {
                runAvailableEliteEquipments = AllEliteEquipments.Where(i => !run.IsEquipmentExpansionLocked(i)).OrderBy(i => i).ToArray();
            };

            Run.onRunDestroyGlobal += _ =>
            {
                runAvailableEliteEquipments = [];
            };
        }

        public static bool HasAnyAvailableEliteEquipments => runAvailableEliteEquipments.Length > 0;

        public static EquipmentIndex GetRandomEliteEquipmentIndex()
        {
            return GetRandomEliteEquipmentIndex(RoR2Application.rng);
        }

        public static EquipmentIndex GetRandomEliteEquipmentIndex(Xoroshiro128Plus rng)
        {
            if (!HasAnyAvailableEliteEquipments)
                return EquipmentIndex.None;

            return rng.NextElementUniform(runAvailableEliteEquipments);
        }

        public static bool IsEliteEquipment(EquipmentIndex equipmentIndex)
        {
            return Array.BinarySearch(baseEliteEquipments, equipmentIndex) >= 0;
        }

        public static EquipmentIndex SelectEliteEquipment(bool allowDirectorUnavailableElites)
        {
            return SelectEliteEquipment(RoR2Application.rng, allowDirectorUnavailableElites);
        }

        public static EquipmentIndex SelectEliteEquipment(Xoroshiro128Plus rng, bool allowDirectorUnavailableElites)
        {
            EquipmentIndex[] eliteEquipments = GetEliteEquipments(allowDirectorUnavailableElites);
            if (eliteEquipments.Length > 0)
            {
                return rng.NextElementUniform(eliteEquipments);
            }
            else
            {
                return EquipmentIndex.None;
            }
        }

        public static EquipmentIndex[] GetEliteEquipments(bool allowDirectorUnavailableElites)
        {
            return Array.ConvertAll(GetElites(allowDirectorUnavailableElites), e => EliteCatalog.GetEliteDef(e).eliteEquipmentDef.equipmentIndex);
        }

        public static EliteIndex[] GetElites(bool allowDirectorUnavailableElites)
        {
            if (!allowDirectorUnavailableElites)
            {
                CombatDirector.EliteTierDef[] availableEliteTiers = CombatDirector.eliteTiers.Where(e => e.eliteTypes.All(ed => ed) && e.CanSelect(SpawnCard.EliteRules.Default)).ToArray();
                if (availableEliteTiers.Length > 0)
                {
                    return availableEliteTiers.SelectMany(t => t.eliteTypes).Distinct().Select(e => e.eliteIndex).ToArray();
                }

                Log.Warning("No available elites, using full list");
            }

            return EliteCatalog.eliteList.ToArray();
        }
    }
}
