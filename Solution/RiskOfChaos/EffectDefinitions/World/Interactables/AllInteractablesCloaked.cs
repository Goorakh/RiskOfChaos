﻿using RiskOfChaos.Collections;
using RiskOfChaos.Components;
using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Patches;
using RiskOfChaos.Trackers;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.Hologram;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.World.Interactables
{
    [ChaosTimedEffect("all_interactables_cloaked", 90f, AllowDuplicates = false)]
    public sealed class AllInteractablesCloaked : MonoBehaviour
    {
        [AddressableReference("RoR2/Base/Common/matCloakedEffect.mat")]
        static readonly Material _cloakedMaterial;

        readonly ClearingObjectList<CloakedInteractableController> _cloakControllers = [];

        void Start()
        {
            List<ObjectSpawnCardTracker> spawnedObjects = InstanceTracker.GetInstancesList<ObjectSpawnCardTracker>();
            _cloakControllers.EnsureCapacity(spawnedObjects.Count);
            foreach (ObjectSpawnCardTracker spawnedObject in spawnedObjects)
            {
                if (spawnedObject.SpawnCard is InteractableSpawnCard)
                {
                    tryAddCloakedObject(spawnedObject.gameObject);
                }
            }

            List<PurchaseInteraction> purchaseInteractions = InstanceTracker.GetInstancesList<PurchaseInteraction>();
            _cloakControllers.EnsureCapacity(_cloakControllers.Count + purchaseInteractions.Count);
            foreach (PurchaseInteraction purchaseInteraction in purchaseInteractions)
            {
                tryCloakPurchaseInteraction(purchaseInteraction);
            }

            SpawnCard.onSpawnedServerGlobal += SpawnCard_onSpawnedServerGlobal;
            PurchaseInteractionHooks.OnPurchaseInteractionStartGlobal += tryCloakPurchaseInteraction;
        }

        void OnDestroy()
        {
            SpawnCard.onSpawnedServerGlobal -= SpawnCard_onSpawnedServerGlobal;
            PurchaseInteractionHooks.OnPurchaseInteractionStartGlobal -= tryCloakPurchaseInteraction;

            _cloakControllers.ClearAndDispose(true);
        }

        void SpawnCard_onSpawnedServerGlobal(SpawnCard.SpawnResult result)
        {
            if (result.success && result.spawnRequest.spawnCard is InteractableSpawnCard)
            {
                GameObject spawnedObject = result.spawnedInstance;
                RoR2Application.onNextUpdate += () =>
                {
                    tryAddCloakedObject(spawnedObject);
                };
            }
        }

        void tryCloakPurchaseInteraction(PurchaseInteraction purchaseInteraction)
        {
            if (!purchaseInteraction.GetComponent<ObjectSpawnCardTracker>())
            {
                tryAddCloakedObject(purchaseInteraction.gameObject);
            }
        }

        void tryAddCloakedObject(GameObject obj)
        {
            if (!obj)
                return;

            IList<CloakedInteractableController> cloakControllers = CloakedInteractableController.TryAddTo(obj);
            if (cloakControllers.Count > 0)
            {
                _cloakControllers.AddRange(cloakControllers);
            }
        }

        class CloakedInteractableController : MonoBehaviour
        {
            MaterialOverride _materialOverride;

            HologramProjector _hologramProjector;
            bool _hologramProjectorWasEnabled;

            Light[] _enabledLights;

            void Awake()
            {
                GameObject modelRoot = gameObject;
                if (TryGetComponent(out ModelLocator modelLocator))
                {
                    Transform modelRootTransform = modelLocator.modelTransform;

                    if (modelRootTransform)
                    {
                        modelRoot = modelRootTransform.gameObject;
                    }
                }

                _materialOverride = modelRoot.AddComponent<MaterialOverride>();
                _materialOverride.IgnoreDecals = true;
                _materialOverride.OverrideMaterial = _cloakedMaterial;

                Light[] lights = modelRoot.GetComponentsInChildren<Light>();
                List<Light> enabledLights = new List<Light>(lights.Length);
                foreach (Light light in lights)
                {
                    if (light.enabled)
                    {
                        light.enabled = false;
                        enabledLights.Add(light);
                    }
                }

                _enabledLights = [.. enabledLights];

                _hologramProjector = GetComponent<HologramProjector>();
                if (_hologramProjector)
                {
                    _hologramProjector.DestroyHologram();

                    if (_hologramProjector.enabled)
                    {
                        _hologramProjectorWasEnabled = true;
                        _hologramProjector.enabled = false;
                    }
                }
            }

            void OnDestroy()
            {
                Destroy(_materialOverride);

                if (_hologramProjector)
                {
                    if (_hologramProjectorWasEnabled)
                    {
                        _hologramProjector.enabled = true;
                    }
                }

                foreach (Light light in _enabledLights)
                {
                    if (light)
                    {
                        light.enabled = true;
                    }
                }
            }

            public static IList<CloakedInteractableController> TryAddTo(GameObject obj)
            {
                if (!obj)
                    return [];

                if (obj.GetComponent<CloakedInteractableController>())
                    return [];

                List<CloakedInteractableController> cloakControllers = [obj.AddComponent<CloakedInteractableController>()];

                if (obj.TryGetComponent(out MultiShopController multiShopController))
                {
                    cloakControllers.EnsureCapacity(cloakControllers.Count + multiShopController.terminalGameObjects.Length);
                    for (int i = 0; i < multiShopController.terminalGameObjects.Length; i++)
                    {
                        cloakControllers.AddRange(TryAddTo(multiShopController.terminalGameObjects[i]));
                    }
                }

                return cloakControllers;
            }
        }
    }
}
