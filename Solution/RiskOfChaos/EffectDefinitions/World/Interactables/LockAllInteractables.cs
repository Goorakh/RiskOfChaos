using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System.Collections;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.EffectDefinitions.World.Interactables
{
    [ChaosTimedEffect("lock_all_interactables", 45f, AllowDuplicates = false)]
    [RequiredComponents(typeof(OutsideInteractableLocker))]
    public sealed class LockAllInteractables : MonoBehaviour
    {
        [PrefabInitializer]
        static IEnumerator InitPrefab(PrefabInitializerArgs args)
        {
            OutsideInteractableLocker outsideInteractableLocker = args.Prefab.GetComponent<OutsideInteractableLocker>();
            outsideInteractableLocker.radius = 0f;

            AsyncOperationHandle<GameObject> purchaseLockLoad = AddressableUtil.LoadTempAssetAsync<GameObject>(AddressableGuids.RoR2_Base_Teleporters_PurchaseLock_prefab);
            purchaseLockLoad.OnSuccess(p => outsideInteractableLocker.lockPrefab = p);

            while (!purchaseLockLoad.IsDone)
            {
                args.ProgressReceiver.Report(purchaseLockLoad.PercentComplete);
                yield return null;
            }
        }

        void Start()
        {
            OutsideInteractableLocker outsideInteractableLocker = GetComponent<OutsideInteractableLocker>();
            if (outsideInteractableLocker)
            {
                // Fix stupid null collections
                outsideInteractableLocker.lockObjectMap ??= [];
                outsideInteractableLocker.eggLockInfoMap ??= [];
            }
        }
    }
}
