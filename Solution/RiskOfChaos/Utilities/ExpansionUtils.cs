using HG.Coroutines;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.ExpansionManagement;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.Utilities
{
    public static class ExpansionUtils
    {
        public static ExpansionDef DLC1 { get; private set; }

        public static ExpansionDef DLC2 { get; private set; }

        public static ResourceAvailability Availability = new ResourceAvailability();

        [SystemInitializer]
        static IEnumerator Init()
        {
            ParallelCoroutine parallelCoroutine = new ParallelCoroutine();

            AsyncOperationHandle<ExpansionDef> dlc1Load = AddressableUtil.LoadAssetAsync<ExpansionDef>(AddressableGuids.RoR2_DLC1_Common_DLC1_asset);
            dlc1Load.OnSuccess(dlc1 => DLC1 = dlc1);
            parallelCoroutine.Add(dlc1Load);

            AsyncOperationHandle<ExpansionDef> dlc2Load = AddressableUtil.LoadAssetAsync<ExpansionDef>(AddressableGuids.RoR2_DLC2_Common_DLC2_asset);
            dlc2Load.OnSuccess(dlc2 => DLC2 = dlc2);
            parallelCoroutine.Add(dlc2Load);

            yield return parallelCoroutine;

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
