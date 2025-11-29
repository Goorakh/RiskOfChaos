using RiskOfChaos.Components;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Patches;
using RiskOfChaos.Trackers;
using RoR2;
using RoR2.ContentManagement;
using RoR2.Hologram;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RiskOfChaos.EffectDefinitions.World.Interactables
{
    [ChaosTimedEffect("all_interactables_cloaked", 90f, AllowDuplicates = false)]
    public sealed class AllInteractablesCloaked : MonoBehaviour
    {
        ChaosEffectComponent _effectComponent;

        AssetOrDirectReference<Material> _cloakedMaterialReference;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();

            _cloakedMaterialReference = new AssetOrDirectReference<Material>
            {
                unloadType = AsyncReferenceHandleUnloadType.AtWill,
                address = new AssetReferenceT<Material>(AddressableGuids.RoR2_Base_Common_matCloakedEffect_mat)
            };
        }

        void Start()
        {
            foreach (ObjectSpawnCardTracker spawnedObject in InstanceTracker.GetInstancesList<ObjectSpawnCardTracker>())
            {
                if (spawnedObject.SpawnCard is InteractableSpawnCard)
                {
                    tryAddCloakedObject(spawnedObject.gameObject);
                }
            }

            foreach (PurchaseInteraction purchaseInteraction in InstanceTracker.GetInstancesList<PurchaseInteraction>())
            {
                tryCloakPurchaseInteraction(purchaseInteraction);
            }

            SpawnCard.onSpawnedServerGlobal += onSpawnCardSpawnedServerGlobal;
            PurchaseInteractionHooks.OnPurchaseInteractionStartGlobal += tryCloakPurchaseInteraction;
        }

        void OnDestroy()
        {
            SpawnCard.onSpawnedServerGlobal -= onSpawnCardSpawnedServerGlobal;
            PurchaseInteractionHooks.OnPurchaseInteractionStartGlobal -= tryCloakPurchaseInteraction;

            _cloakedMaterialReference?.Reset();
        }

        void onSpawnCardSpawnedServerGlobal(SpawnCard.SpawnResult result)
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

            CloakedInteractableController.TryAddTo(obj, this);
        }

        sealed class CloakedInteractableController : MonoBehaviour
        {
            AllInteractablesCloaked _owner;

            SpecialObjectAttributes _specialObjectAttributes;

            MaterialOverride _materialOverride;

            void Start()
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
                _materialOverride.OverrideMaterial = _owner._cloakedMaterialReference.WaitForCompletion();

                _specialObjectAttributes = GetComponent<SpecialObjectAttributes>();

                VehicleSeat.onPassengerEnterGlobal += onPassengerEnterGlobal;
                VehicleSeat.onPassengerExitGlobal += onPassengerExitGlobal;

                setVisualsActive(false);

                _owner._effectComponent.OnEffectEnd += onOwnerEffectEnd;
            }

            void OnDestroy()
            {
                Destroy(_materialOverride);

                if (!VehicleSeat.FindVehicleSeatWithPassenger(gameObject))
                {
                    setVisualsActive(true);
                }

                VehicleSeat.onPassengerEnterGlobal -= onPassengerEnterGlobal;
                VehicleSeat.onPassengerExitGlobal -= onPassengerExitGlobal;
            }

            void onOwnerEffectEnd(ChaosEffectComponent effectComponent)
            {
                Destroy(this);
            }

            void setVisualsActive(bool active)
            {
                setSpecialObjectVisualsActive(_specialObjectAttributes, active);
            }

            static void setSpecialObjectVisualsActive(SpecialObjectAttributes specialObjectAttributes, bool active)
            {
                if (!specialObjectAttributes)
                    return;

                foreach (Light light in specialObjectAttributes.lightsToDisable)
                {
                    if (light)
                    {
                        light.enabled = active;
                    }
                }

                foreach (MonoBehaviour behaviour in specialObjectAttributes.behavioursToDisable)
                {
                    if (behaviour && behaviour is HologramProjector hologramProjector)
                    {
                        if (hologramProjector.hologramContentInstance)
                        {
                            hologramProjector.hologramContentInstance.SetActive(active);
                        }

                        behaviour.enabled = active;
                    }
                }

                foreach (PickupDisplay pickupDisplay in specialObjectAttributes.pickupDisplaysToDisable)
                {
                    if (pickupDisplay)
                    {
                        if (pickupDisplay.highlight)
                        {
                            pickupDisplay.highlight.enabled = active;
                        }
                    }
                }

                foreach (SpecialObjectAttributes childObjectAttributes in specialObjectAttributes.childSpecialObjectAttributes)
                {
                    setSpecialObjectVisualsActive(childObjectAttributes, active);
                }
            }

            void onPassengerEnterGlobal(VehicleSeat seat, GameObject passengerObject)
            {
                if (passengerObject == gameObject)
                {
                    setVisualsActive(false);
                }
            }

            void onPassengerExitGlobal(VehicleSeat seat, GameObject passengerObject)
            {
                if (passengerObject == gameObject)
                {
                    setVisualsActive(false);
                }
            }

            static CloakedInteractableController addTo(GameObject obj, AllInteractablesCloaked owner)
            {
                CloakedInteractableController cloakedInteractableController = obj.AddComponent<CloakedInteractableController>();
                cloakedInteractableController._owner = owner;
                return cloakedInteractableController;
            }

            public static void TryAddTo(GameObject obj, AllInteractablesCloaked owner)
            {
                if (!obj)
                    return;

                if (obj.GetComponent<CloakedInteractableController>())
                    return;

                if (obj.TryGetComponent(out ShopTerminalBehavior shopTerminalBehavior) && obj.GetComponentInParent<MultiShopController>())
                    return;

                addTo(obj, owner);

                if (obj.TryGetComponent(out MultiShopController multiShopController))
                {
                    foreach (GameObject terminalObject in multiShopController.terminalGameObjects)
                    {
                        if (!terminalObject.transform.IsChildOf(obj.transform))
                        {
                            TryAddTo(terminalObject, owner);
                        }
                    }
                }
            }
        }
    }
}
