using HG.Coroutines;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
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
            static void addExpansionRequirement(string prefabAssetGuid, ExpansionIndex expansionIndex, List<AsyncOperationHandle> operationsList)
            {
                if (string.IsNullOrWhiteSpace(prefabAssetGuid))
                    throw new System.ArgumentException($"'{nameof(prefabAssetGuid)}' cannot be null or whitespace.", nameof(prefabAssetGuid));

                if (expansionIndex == ExpansionIndex.None)
                {
                    Log.Error($"Null expansion! {nameof(prefabAssetGuid)}={prefabAssetGuid}");
                    return;
                }

                AsyncOperationHandle<GameObject> loadHandle = AddressableUtil.LoadTempAssetAsync<GameObject>(prefabAssetGuid);
                loadHandle.OnSuccess(prefab =>
                {
                    ExpansionDef expansionDef = ExpansionUtils.GetExpansionDef(expansionIndex);

                    if (ExpansionUtils.GetObjectRequiredExpansions(prefab).Contains(expansionDef))
                    {
                        Log.Debug($"Already has required expansion {expansionDef.name}");
                        return;
                    }

                    prefab.AddComponent<ExpansionRequirementComponent>().requiredExpansion = expansionDef;

                    Log.Debug($"Added expansion requirement {expansionDef.name} to {prefab.name}");
                });

                operationsList.Add(loadHandle);
            }

            List<AsyncOperationHandle> asyncOperations = [];
            addExpansionRequirement(AddressableGuids.RoR2_DLC1_DroneCommander_DroneCommanderBody_prefab, ExpansionUtils.DLC1, asyncOperations);
            addExpansionRequirement(AddressableGuids.RoR2_DLC1_Assassin2_Assassin2Body_prefab, ExpansionUtils.DLC1, asyncOperations);

            ParallelCoroutine parallelCoroutine = new ParallelCoroutine();
            parallelCoroutine.AddRange(asyncOperations);

            return parallelCoroutine;
        }
    }
}
