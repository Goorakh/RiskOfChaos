using RiskOfChaos.Components;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Patches;
using RiskOfChaos.Trackers;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.EffectDefinitions.World.Pickups
{
    [IncompatibleEffects(typeof(GenericAttractPickupsEffect))]
    public sealed class GenericAttractPickupsEffect : MonoBehaviour
    {
        [SystemInitializer]
        static void Init()
        {
            static void addNetworkTransform(string prefabAssetPath)
            {
                AsyncOperationHandle<GameObject> prefabAssetLoad = Addressables.LoadAssetAsync<GameObject>(prefabAssetPath);
                prefabAssetLoad.OnSuccess(prefab =>
                {
                    ProjectileNetworkTransform networkTransform = prefab.GetComponent<ProjectileNetworkTransform>();
                    if (networkTransform || prefab.GetComponent<NetworkTransform>())
                    {
                        Log.Info($"{prefab.name} ({prefabAssetPath}) already has NetworkTransform component, skipping");
                        return;
                    }

                    if (!prefab.GetComponent<NetworkIdentity>())
                    {
                        Log.Error($"{prefab.name} ({prefabAssetPath}) is not a networked object");
                        return;
                    }

                    if (!AttractToPlayers.CanAddComponent(prefab))
                    {
                        Log.Error($"{prefab.name} ({prefabAssetPath}) is invalid for component");
                        return;
                    }

                    networkTransform = prefab.AddComponent<ProjectileNetworkTransform>();
                    networkTransform.positionTransmitInterval = 1f / 15f;
                    networkTransform.allowClientsideCollision = true;

                    Log.Debug($"Added network transform component to {prefab.name} ({prefabAssetPath})");
                });
            }

            addNetworkTransform("RoR2/Base/Common/GenericPickup.prefab");
            addNetworkTransform("RoR2/Base/Command/CommandCube.prefab");

            addNetworkTransform("RoR2/DLC1/OptionPickup/OptionPickup.prefab");

            addNetworkTransform("RoR2/DLC2/FragmentPotentialPickup.prefab");
        }

        readonly List<AttractToPlayers> _attractComponents = [];
        public ReadOnlyCollection<AttractToPlayers> AttractComponents { get; private set; }

        readonly List<OnDestroyCallback> _destroyCallbacks = [];

        public event Action<AttractToPlayers> SetupAttractComponent;

        bool _trackedObjectDestroyed;

        void Awake()
        {
            AttractComponents = _attractComponents.AsReadOnly();
        }

        void Start()
        {
            if (NetworkServer.active)
            {
                List<MonoBehaviour> pickupControllerComponents = [
                    .. InstanceTracker.GetInstancesList<PickupDropletControllerTracker>(),
                    .. InstanceTracker.GetInstancesList<GenericPickupController>(),
                    .. InstanceTracker.GetInstancesList<PickupPickerController>()
                ];

                _attractComponents.EnsureCapacity(pickupControllerComponents.Count);
                _destroyCallbacks.EnsureCapacity(pickupControllerComponents.Count);

                foreach (MonoBehaviour pickupControllerComponent in pickupControllerComponents)
                {
                    tryAddComponentTo(pickupControllerComponent.gameObject);
                }

                PickupDropletControllerTracker.OnPickupDropletControllerStartGlobal += onPickupDropletControllerStartGlobal;
                GenericPickupControllerHooks.OnGenericPickupControllerStartGlobal += onGenericPickupControllerStartGlobal;
                PickupPickerControllerHooks.OnPickupPickerControllerAwakeGlobal += onPickupPickerControllerAwakeGlobal;
            }
        }

        void FixedUpdate()
        {
            if (_trackedObjectDestroyed)
            {
                _trackedObjectDestroyed = false;

                UnityObjectUtils.RemoveAllDestroyed(_destroyCallbacks);

                int removedAttractComponents = UnityObjectUtils.RemoveAllDestroyed(_attractComponents);
                Log.Debug($"Cleared {removedAttractComponents} destroyed attract component(s)");
            }
        }

        void OnDestroy()
        {
            PickupDropletControllerTracker.OnPickupDropletControllerStartGlobal -= onPickupDropletControllerStartGlobal;
            GenericPickupControllerHooks.OnGenericPickupControllerStartGlobal -= onGenericPickupControllerStartGlobal;
            PickupPickerControllerHooks.OnPickupPickerControllerAwakeGlobal -= onPickupPickerControllerAwakeGlobal;

            foreach (AttractToPlayers attractComponent in _attractComponents)
            {
                if (attractComponent)
                {
                    Destroy(attractComponent);
                }
            }

            _attractComponents.Clear();

            foreach (OnDestroyCallback destroyCallback in _destroyCallbacks)
            {
                if (destroyCallback)
                {
                    OnDestroyCallback.RemoveCallback(destroyCallback);
                }
            }

            _destroyCallbacks.Clear();
        }

        void onPickupDropletControllerStartGlobal(PickupDropletController pickupDropletController)
        {
            tryAddComponentTo(pickupDropletController.gameObject);
        }

        void onGenericPickupControllerStartGlobal(GenericPickupController genericPickupController)
        {
            tryAddComponentTo(genericPickupController.gameObject);
        }

        void onPickupPickerControllerAwakeGlobal(PickupPickerController pickupPickerController)
        {
            tryAddComponentTo(pickupPickerController.gameObject);
        }

        void tryAddComponentTo(GameObject pickupControllerObj)
        {
            AttractToPlayers attractComponent = AttractToPlayers.TryAddComponent(pickupControllerObj);
            if (attractComponent)
            {
                SetupAttractComponent?.Invoke(attractComponent);
                _attractComponents.Add(attractComponent);

                OnDestroyCallback destroyCallback = OnDestroyCallback.AddCallback(pickupControllerObj, _ =>
                {
                    _trackedObjectDestroyed = true;
                });

                _destroyCallbacks.Add(destroyCallback);
            }
        }
    }
}
