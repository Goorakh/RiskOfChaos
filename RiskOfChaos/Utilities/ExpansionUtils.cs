using RoR2;
using RoR2.ExpansionManagement;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RiskOfChaos.Utilities
{
    public static class ExpansionUtils
    {
        public static bool DLC1Enabled
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => IsExpansionEnabled(DLC1);
        }

        public static readonly ExpansionDef DLC1 = Addressables.LoadAssetAsync<ExpansionDef>("RoR2/DLC1/Common/DLC1.asset").WaitForCompletion();

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
