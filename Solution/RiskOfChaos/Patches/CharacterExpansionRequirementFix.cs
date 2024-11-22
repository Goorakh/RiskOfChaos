using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.ExpansionManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.Patches
{
    static class CharacterExpansionRequirementFix
    {
        [SystemInitializer(typeof(ExpansionUtils))]
        static IEnumerator Init()
        {
            List<AsyncOperationHandle> asyncOperations = [];

            static void addExpansionRequirement(string assetPath, ExpansionDef expansionDef, List<AsyncOperationHandle> operationsList)
            {
                if (string.IsNullOrWhiteSpace(assetPath))
                    throw new System.ArgumentException($"'{nameof(assetPath)}' cannot be null or whitespace.", nameof(assetPath));

                if (!expansionDef)
                {
                    Log.Error($"Null expansion! {nameof(assetPath)}={assetPath}");
                    return;
                }

                AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(assetPath);
                handle.OnSuccess(prefab =>
                {
                    if (ExpansionUtils.GetObjectRequiredExpansions(prefab).Contains(expansionDef))
                    {
#if DEBUG
                        Log.Debug($"Already has required expansion {expansionDef.name}");
#endif
                        return;
                    }

                    prefab.AddComponent<ExpansionRequirementComponent>().requiredExpansion = expansionDef;

#if DEBUG
                    Log.Debug($"Added expansion requirement {expansionDef.name} to {prefab.name}");
#endif
                });

                operationsList.Add(handle);
            }

            addExpansionRequirement("RoR2/DLC2/Scorchling/ScorchlingBody.prefab", ExpansionUtils.DLC2, asyncOperations);
            addExpansionRequirement("RoR2/DLC1/DroneCommander/DroneCommanderBody.prefab", ExpansionUtils.DLC1, asyncOperations);
            addExpansionRequirement("RoR2/DLC1/Assassin2/Assassin2Body.prefab", ExpansionUtils.DLC1, asyncOperations);

            return asyncOperations.WaitForAllLoaded();
        }
    }
}
