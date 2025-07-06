using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.ExpansionManagement;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace RiskOfChaos.Utilities
{
    public static class ExpansionUtils
    {
        public static ExpansionIndex DLC1 { get; private set; } = ExpansionIndex.None;

        public static ExpansionIndex DLC2 { get; private set; } = ExpansionIndex.None;

        public static ResourceAvailability Availability = new ResourceAvailability();

        [SystemInitializer(typeof(ExpansionCatalog))]
        static void Init()
        {
            for (int i = 0; i < ExpansionCatalog.expansionDefs.Length; i++)
            {
                ExpansionDef expansionDef = ExpansionCatalog.expansionDefs[i];
                switch (expansionDef.name)
                {
                    case "DLC1":
                        DLC1 = expansionDef.expansionIndex;
                        break;
                    case "DLC2":
                        DLC2 = expansionDef.expansionIndex;
                        break;
                }
            }

            Availability.MakeAvailable();
        }

        public static bool DLC1Enabled
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => IsExpansionEnabled(DLC1);
        }

        public static bool DLC2Enabled
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => IsExpansionEnabled(DLC2);
        }

        public static ExpansionDef GetExpansionDef(ExpansionIndex index)
        {
            if ((uint)index >= ExpansionCatalog.expansionDefs.Length)
                return null;

            return ExpansionCatalog.expansionDefs[(int)index];
        }

        public static bool IsExpansionEnabled(ExpansionIndex expansionIndex)
        {
            return IsExpansionEnabled(GetExpansionDef(expansionIndex));
        }

        public static bool IsExpansionEnabled(ExpansionDef expansionDef)
        {
            return !expansionDef || (Run.instance && Run.instance.IsExpansionEnabled(expansionDef));
        }

        public static bool AllExpansionsEnabled(IEnumerable<ExpansionIndex> expansions)
        {
            foreach (ExpansionIndex expansion in expansions)
            {
                if (!IsExpansionEnabled(expansion))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool AllExpansionsEnabled(IEnumerable<ExpansionDef> expansions)
        {
            foreach (ExpansionDef expansion in expansions)
            {
                if (!IsExpansionEnabled(expansion))
                {
                    return false;
                }
            }

            return true;
        }

        static void addAllRequiredExpansions(GameObject obj, List<ExpansionDef> dest)
        {
            if (!obj)
                return;

            ExpansionRequirementComponent[] expansionRequirements = obj.GetComponents<ExpansionRequirementComponent>();

            dest.EnsureCapacity(dest.Count + expansionRequirements.Length);

            foreach (ExpansionRequirementComponent expansionRequirement in expansionRequirements)
            {
                if (expansionRequirement.requiredExpansion)
                {
                    dest.Add(expansionRequirement.requiredExpansion);
                }
            }

            if (obj.TryGetComponent(out CharacterMaster master) && master.bodyPrefab)
            {
                addAllRequiredExpansions(master.bodyPrefab, dest);
            }
        }

        public static IReadOnlyList<ExpansionDef> GetObjectRequiredExpansions(GameObject obj)
        {
            if (!obj)
                return [];

            List<ExpansionDef> requiredExpansions = [];
            addAllRequiredExpansions(obj, requiredExpansions);

            requiredExpansions.TrimExcess();

            return requiredExpansions.AsReadOnly();
        }

        public static IReadOnlyList<ExpansionIndex> GetObjectRequiredExpansionIndices(GameObject obj)
        {
            if (!obj)
                return [];

            List<ExpansionDef> requiredExpansionDefs = [];
            addAllRequiredExpansions(obj, requiredExpansionDefs);
            if (requiredExpansionDefs.Count <= 0)
                return [];

            ExpansionIndex[] requiredExpansions = new ExpansionIndex[requiredExpansionDefs.Count];
            for (int i = 0; i < requiredExpansions.Length; i++)
            {
                requiredExpansions[i] = requiredExpansionDefs[i].expansionIndex;
            }

            return requiredExpansions;
        }

        public static bool IsObjectExpansionAvailable(GameObject obj)
        {
            return AllExpansionsEnabled(GetObjectRequiredExpansions(obj));
        }
    }
}
