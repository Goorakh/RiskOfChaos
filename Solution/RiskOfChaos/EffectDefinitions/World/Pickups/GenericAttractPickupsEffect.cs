using RiskOfChaos.Collections;
using RiskOfChaos.Components;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Patches;
using RiskOfChaos.Trackers;
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

        readonly ClearingObjectList<AttractToPlayers> _attractComponents = [];
        public ReadOnlyCollection<AttractToPlayers> AttractComponents { get; private set; }

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

                foreach (MonoBehaviour pickupControllerComponent in pickupControllerComponents)
                {
                    tryAddComponentTo(pickupControllerComponent.gameObject);
                }

                PickupDropletControllerTracker.OnPickupDropletControllerStartGlobal += onPickupDropletControllerStartGlobal;
                GenericPickupControllerHooks.OnGenericPickupControllerStartGlobal += onGenericPickupControllerStartGlobal;
                PickupPickerControllerHooks.OnPickupPickerControllerAwakeGlobal += onPickupPickerControllerAwakeGlobal;
            }
        }

        void OnDestroy()
        {
            PickupDropletControllerTracker.OnPickupDropletControllerStartGlobal -= onPickupDropletControllerStartGlobal;
            GenericPickupControllerHooks.OnGenericPickupControllerStartGlobal -= onGenericPickupControllerStartGlobal;
            PickupPickerControllerHooks.OnPickupPickerControllerAwakeGlobal -= onPickupPickerControllerAwakeGlobal;

            _attractComponents.ClearAndDispose(true);
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
            }
        }
    }
}
