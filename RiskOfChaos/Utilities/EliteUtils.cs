using RoR2;
using System;
using System.Linq;

namespace RiskOfChaos.Utilities
{
    public static class EliteUtils
    {
        static EquipmentIndex[] _baseEliteEquipments = Array.Empty<EquipmentIndex>();

        [SystemInitializer(typeof(EquipmentCatalog), typeof(EliteCatalog))]
        static void InitBaseEquipments()
        {
            _baseEliteEquipments = EliteCatalog.eliteList
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

                if (Array.BinarySearch(_baseEliteEquipments, eliteEquipmentDef.equipmentIndex) < 0)
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

        static EquipmentIndex[] _runAvailableEliteEquipments = Array.Empty<EquipmentIndex>();

        [SystemInitializer]
        static void Init()
        {
            Run.onRunStartGlobal += run =>
            {
                _runAvailableEliteEquipments = _baseEliteEquipments.Where(i => !run.IsEquipmentExpansionLocked(i)).OrderBy(i => i).ToArray();
            };

            Run.onRunDestroyGlobal += _ =>
            {
                _runAvailableEliteEquipments = Array.Empty<EquipmentIndex>();
            };
        }

        public static bool HasAnyAvailableEliteEquipments => _runAvailableEliteEquipments != null && _runAvailableEliteEquipments.Length > 0;

        public static EquipmentIndex GetRandomEliteEquipmentIndex()
        {
            return GetRandomEliteEquipmentIndex(RoR2Application.rng);
        }

        public static EquipmentIndex GetRandomEliteEquipmentIndex(Xoroshiro128Plus rng)
        {
            if (!HasAnyAvailableEliteEquipments)
                return EquipmentIndex.None;

            return rng.NextElementUniform(_runAvailableEliteEquipments);
        }

        public static bool IsEliteEquipment(EquipmentIndex equipmentIndex)
        {
            return Array.BinarySearch(_baseEliteEquipments, equipmentIndex) >= 0;
        }

        public static EquipmentIndex SelectEliteEquipment(Xoroshiro128Plus rng, bool allowDirectorUnavailableElites)
        {
            if (allowDirectorUnavailableElites)
            {
                return GetRandomEliteEquipmentIndex(rng);
            }
            else
            {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                CombatDirector.EliteTierDef[] eliteTiers = CombatDirector.eliteTiers;
#pragma warning restore Publicizer001 // Accessing a member that was not originally public

                CombatDirector.EliteTierDef[] availableEliteTiers = eliteTiers.Where(e => e.eliteTypes.All(ed => ed) && e.CanSelect(SpawnCard.EliteRules.Default)).ToArray();
                if (availableEliteTiers.Length > 0)
                {
                    CombatDirector.EliteTierDef eliteTier = rng.NextElementUniform(availableEliteTiers);
                    EliteDef eliteDef = eliteTier.GetRandomAvailableEliteDef(rng);
                    if (eliteDef)
                    {
                        return eliteDef.eliteEquipmentDef.equipmentIndex;
                    }
                }

                Log.Warning("No available elites");
                return EquipmentIndex.None;
            }
        }
    }
}
