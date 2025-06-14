using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.ContentManagement;
using RoR2.ExpansionManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.Patches
{
    static class CharacterExpansionRequirementFix
    {
        [SystemInitializer(typeof(ExpansionUtils))]
        static IEnumerator Init()
        {
            List<AsyncOperationHandle> asyncOperations = [];

            static void addExpansionRequirement(string prefabAssetGuid, ExpansionDef expansionDef, List<AsyncOperationHandle> operationsList)
            {
                if (string.IsNullOrWhiteSpace(prefabAssetGuid))
                    throw new System.ArgumentException($"'{nameof(prefabAssetGuid)}' cannot be null or whitespace.", nameof(prefabAssetGuid));

                if (!expansionDef)
                {
                    Log.Error($"Null expansion! {nameof(prefabAssetGuid)}={prefabAssetGuid}");
                    return;
                }

                AsyncOperationHandle<GameObject> handle = AddressableUtil.LoadAssetAsync<GameObject>(prefabAssetGuid, AsyncReferenceHandleUnloadType.Preload);
                handle.OnSuccess(prefab =>
                {
                    if (ExpansionUtils.GetObjectRequiredExpansions(prefab).Contains(expansionDef))
                    {
                        Log.Debug($"Already has required expansion {expansionDef.name}");
                        return;
                    }

                    prefab.AddComponent<ExpansionRequirementComponent>().requiredExpansion = expansionDef;

                    Log.Debug($"Added expansion requirement {expansionDef.name} to {prefab.name}");
                });

                operationsList.Add(handle);
            }

            addExpansionRequirement(AddressableGuids.RoR2_DLC1_DroneCommander_DroneCommanderBody_prefab, ExpansionUtils.DLC1, asyncOperations);
            addExpansionRequirement(AddressableGuids.RoR2_DLC1_Assassin2_Assassin2Body_prefab, ExpansionUtils.DLC1, asyncOperations);

            return asyncOperations.WaitForAllLoaded();
        }
    }
}
