using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosEffect("lock_all_interactables")]
    [ChaosTimedEffect(45f, AllowDuplicates = false)]
    public sealed class LockAllInteractables : TimedEffect
    {
        static GameObject _purchaseLockPrefab;

        [SystemInitializer]
        static void Init()
        {
            _purchaseLockPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Teleporters/PurchaseLock.prefab").WaitForCompletion();
        }

        [EffectCanActivate]
        static bool CanActivate(EffectCanActivateContext context)
        {
            return _purchaseLockPrefab && (!context.IsNow || getAllNonLockedInteractables().Any());
        }

        static IEnumerable<PurchaseInteraction> getAllNonLockedInteractables()
        {
            return InstanceTracker.GetInstancesList<PurchaseInteraction>().Where(p => !p.lockGameObject);
        }

        readonly List<GameObject> _spawnedLockObjects = new List<GameObject>();

        float _lastInteractableCheckTime = float.NegativeInfinity;

        public override void OnStart()
        {
            RoR2Application.onFixedUpdate += fixedUpdate;
        }

        void fixedUpdate()
        {
            const float INTERACTABLE_CHECK_INTERVAL = 1f;
            if (TimeElapsed >= _lastInteractableCheckTime + INTERACTABLE_CHECK_INTERVAL)
            {
                lockAllInteractables();
                _lastInteractableCheckTime = TimeElapsed;
            }
        }

        void lockAllInteractables()
        {
            getAllNonLockedInteractables().TryDo(lockInteractable);
        }

        void lockInteractable(PurchaseInteraction purchaseInteraction)
        {
            GameObject lockObject = GameObject.Instantiate(_purchaseLockPrefab, purchaseInteraction.transform.position, Quaternion.Euler(0f, RNG.RangeFloat(0f, 360f), 0f));
            NetworkServer.Spawn(lockObject);
            purchaseInteraction.NetworklockGameObject = lockObject;

            _spawnedLockObjects.Add(lockObject);
        }

        public override void OnEnd()
        {
            RoR2Application.onFixedUpdate -= fixedUpdate;

            foreach (GameObject lockObject in _spawnedLockObjects)
            {
                if (lockObject)
                {
                    NetworkServer.Destroy(lockObject);
                }
            }

            _spawnedLockObjects.Clear();
        }
    }
}
