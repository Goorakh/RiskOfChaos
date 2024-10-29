using RoR2;
using RoR2.ExpansionManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.Utilities
{
    public static class ExpansionUtils
    {
        public static ExpansionDef DLC1 { get; private set; }

        public static ExpansionDef DLC2 { get; private set; }

        [SystemInitializer]
        static IEnumerator Init()
        {
            List<AsyncOperationHandle> asyncOperations = new List<AsyncOperationHandle>(2);

            AsyncOperationHandle<ExpansionDef> dlc1Load = Addressables.LoadAssetAsync<ExpansionDef>("RoR2/DLC1/Common/DLC1.asset");
            dlc1Load.OnSuccess(dlc1 => DLC1 = dlc1);

            asyncOperations.Add(dlc1Load);

            AsyncOperationHandle<ExpansionDef> dlc2Load = Addressables.LoadAssetAsync<ExpansionDef>("RoR2/DLC2/Common/DLC2.asset");
            dlc2Load.OnSuccess(dlc2 => DLC2 = dlc2);

            asyncOperations.Add(dlc2Load);

            yield return asyncOperations.WaitForAllLoaded();
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

        public static bool IsExpansionEnabled(ExpansionDef expansionDef)
        {
            return expansionDef && Run.instance && Run.instance.IsExpansionEnabled(expansionDef);
        }

        public static bool IsObjectExpansionAvailable(GameObject obj)
        {
            if (!obj || !Run.instance)
                return false;

            return obj.GetComponents<ExpansionRequirementComponent>().All(requirement =>
            {
                return !requirement.requiredExpansion || Run.instance.IsExpansionEnabled(requirement.requiredExpansion);
            });
        }

        public static bool IsCharacterMasterExpansionAvailable(GameObject masterPrefabObj)
        {
            if (!IsObjectExpansionAvailable(masterPrefabObj))
                return false;

            if (!masterPrefabObj.TryGetComponent(out CharacterMaster masterPrefab))
            {
                Log.Warning($"Object {masterPrefabObj} has no CharacterMaster component");
                return false;
            }

            return IsObjectExpansionAvailable(masterPrefab.bodyPrefab);
        }
    }
}
