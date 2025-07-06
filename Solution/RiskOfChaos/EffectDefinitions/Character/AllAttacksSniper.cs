using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2.ContentManagement;
using System.Collections;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosTimedEffect("all_attacks_sniper", 90f, AllowDuplicates = false)]
    public sealed class AllAttacksSniper : MonoBehaviour
    {
        [InitEffectInfo]
        public static readonly TimedEffectInfo EffectInfo;

        [PrefabInitializer]
        static IEnumerator InitPrefab(GameObject prefab)
        {
            AsyncOperationHandle<GameObject> railgunnerBodyLoad = AddressableUtil.LoadAssetAsync<GameObject>(AddressableGuids.RoR2_DLC1_Railgunner_RailgunnerBody_prefab, AsyncReferenceHandleUnloadType.Preload);
            railgunnerBodyLoad.OnSuccess(railgunnerBodyPrefab =>
            {
                if (railgunnerBodyPrefab && railgunnerBodyPrefab.TryGetComponent(out AkBank railgunnerBank) && railgunnerBank.data?.ObjectReference)
                {
                    AkBank effectBank = prefab.AddComponent<AkBank>();
                    effectBank.data = railgunnerBank.data;
                    effectBank.triggerList = [AkTriggerHandler.START_TRIGGER_ID];
                    effectBank.unloadTriggerList = [AkTriggerHandler.DESTROY_TRIGGER_ID];
                }
                else
                {
                    Log.Error("Failed to find railgunner sound bank");
                }
            });

            return railgunnerBodyLoad;
        }
    }
}
