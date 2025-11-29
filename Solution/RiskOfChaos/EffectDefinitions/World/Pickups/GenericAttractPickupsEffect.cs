using RiskOfChaos.Components;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Patches;
using RiskOfChaos.Trackers;
using RoR2;
using RoR2.ContentManagement;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Pickups
{
    [IncompatibleEffects(typeof(GenericAttractPickupsEffect))]
    public sealed class GenericAttractPickupsEffect : MonoBehaviour
    {
        [SystemInitializer]
        static void Init()
        {
            foreach (GameObject prefab in ContentManager.networkedObjectPrefabs)
            {
                if (prefab.GetComponent<GenericPickupController>() ||
                    prefab.GetComponent<PickupPickerController>() ||
                    prefab.GetComponent<PickupDropletController>())
                {
                    if (AttractToPlayers.CanAddComponent(prefab))
                    {
                        ProjectileNetworkTransform networkTransform = prefab.GetComponent<ProjectileNetworkTransform>();
                        if (networkTransform || prefab.GetComponent<NetworkTransform>())
                        {
                            Log.Info($"{prefab.name} already has NetworkTransform component, skipping");
                            continue;
                        }

                        if (!prefab.GetComponent<NetworkIdentity>())
                        {
                            Log.Error($"{prefab.name} is not a networked object");
                            continue;
                        }

                        if (!AttractToPlayers.CanAddComponent(prefab))
                        {
                            Log.Error($"{prefab.name} is invalid for component");
                            continue;
                        }

                        networkTransform = prefab.AddComponent<ProjectileNetworkTransform>();
                        networkTransform.positionTransmitInterval = 1f / 10f;
                        networkTransform.allowClientsideCollision = true;

                        Log.Debug($"Added network transform component to {prefab.name}");
                    }
                }
            }
        }

        ChaosEffectComponent _effectComponent;

        public event Action<AttractToPlayers> SetupAttractComponent;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        void Start()
        {
            if (NetworkServer.active)
            {
                IEnumerable<MonoBehaviour> pickupControllerComponents = [
                    .. InstanceTracker.GetInstancesList<PickupDropletControllerTracker>(),
                    .. InstanceTracker.GetInstancesList<GenericPickupController>(),
                    .. InstanceTracker.GetInstancesList<PickupPickerController>()
                ];

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
                attractComponent.OwnerEffectComponent = _effectComponent;
                SetupAttractComponent?.Invoke(attractComponent);
            }
        }
    }
}
