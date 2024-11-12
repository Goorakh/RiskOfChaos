using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.EffectDefinitions.World.Interactables
{
    [ChaosTimedEffect("lock_all_interactables", 45f, AllowDuplicates = false)]
    public sealed class LockAllInteractables : MonoBehaviour
    {
        static GameObject _purchaseLockPrefab;

        [SystemInitializer]
        static void Init()
        {
            AsyncOperationHandle<GameObject> purchaseLockLoad = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Teleporters/PurchaseLock.prefab");
            purchaseLockLoad.OnSuccess(p => _purchaseLockPrefab = p);
        }

        [EffectCanActivate]
        static bool CanActivate(in EffectCanActivateContext context)
        {
            return _purchaseLockPrefab && (!context.IsNow || InstanceTracker.Any<PurchaseInteraction>());
        }

        readonly List<GameObject> _spawnedLockObjects = [];
        readonly List<OnDestroyCallback> _destroyCallbacks = [];

        bool _trackedObjectDestroyed;

        float _interactableCheckTimer;

        void Start()
        {
            if (!NetworkServer.active)
                return;

            List<PurchaseInteraction> purchaseInteractions = InstanceTracker.GetInstancesList<PurchaseInteraction>();

            _spawnedLockObjects.EnsureCapacity(purchaseInteractions.Count);
            _destroyCallbacks.EnsureCapacity(purchaseInteractions.Count);
        }

        void OnDestroy()
        {
            foreach (OnDestroyCallback destroyCallback in _destroyCallbacks)
            {
                if (destroyCallback)
                {
                    OnDestroyCallback.RemoveCallback(destroyCallback);
                }
            }

            _destroyCallbacks.Clear();

            foreach (GameObject lockObject in _spawnedLockObjects)
            {
                if (lockObject)
                {
                    NetworkServer.Destroy(lockObject);
                }
            }

            _spawnedLockObjects.Clear();
        }

        void FixedUpdate()
        {
            if (!NetworkServer.active)
                return;

            _interactableCheckTimer -= Time.fixedDeltaTime;
            if (_interactableCheckTimer <= 0f)
            {
                _interactableCheckTimer += 1f;
                InstanceTracker.GetInstancesList<PurchaseInteraction>().TryDo(tryLockInteractable);
            }

            if (_trackedObjectDestroyed)
            {
                UnityObjectUtils.RemoveAllDestroyed(_destroyCallbacks);

                int removedLockObjects = UnityObjectUtils.RemoveAllDestroyed(_spawnedLockObjects);
#if DEBUG
                Log.Debug($"Cleared {removedLockObjects} destroyed lock objects");
#endif
            }
        }

        void tryLockInteractable(PurchaseInteraction purchaseInteraction)
        {
            if (!purchaseInteraction.available || purchaseInteraction.lockGameObject)
                return;

            Vector3 lockPosition = purchaseInteraction.transform.position;
            Quaternion lockRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            GameObject lockObject = Instantiate(_purchaseLockPrefab, lockPosition, lockRotation);
            NetworkServer.Spawn(lockObject);
            purchaseInteraction.NetworklockGameObject = lockObject;

            _spawnedLockObjects.Add(lockObject);

            OnDestroyCallback destroyCallback = OnDestroyCallback.AddCallback(lockObject, _ =>
            {
                _trackedObjectDestroyed = true;
            });

            _destroyCallbacks.Add(destroyCallback);
        }
    }
}
