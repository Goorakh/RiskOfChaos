using RiskOfChaos.Content;
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
        [AddressableReference("RoR2/DLC1/Common/DLC1.asset")]
        public static readonly ExpansionDef DLC1;

        [AddressableReference("RoR2/DLC2/Common/DLC2.asset")]
        public static readonly ExpansionDef DLC2;

        public static ResourceAvailability Availability = new ResourceAvailability();

        [SystemInitializer(typeof(AddressableReferenceAttribute))]
        static void Init()
        {
            Availability.MakeAvailable();
        }

        public static bool DLC1Enabled
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => DLC1 && IsExpansionEnabled(DLC1);
        }

        public static bool DLC2Enabled
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => DLC2 && IsExpansionEnabled(DLC2);
        }

        public static bool IsExpansionEnabled(ExpansionDef expansionDef)
        {
            return !expansionDef || (Run.instance && Run.instance.IsExpansionEnabled(expansionDef));
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

        public static bool IsObjectExpansionAvailable(GameObject obj)
        {
            return AllExpansionsEnabled(GetObjectRequiredExpansions(obj));
        }
    }
}
