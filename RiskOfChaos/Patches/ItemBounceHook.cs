using RiskOfChaos.ModifierController.Pickups;
using RoR2;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Patches
{
    static class ItemBounceHook
    {
        static uint bounceCount
        {
            get
            {
                if (PickupModificationManager.Instance)
                {
                    return PickupModificationManager.Instance.BounceCount;
                }
                else
                {
                    return 0;
                }
            }
        }

        [SystemInitializer]
        static void Init()
        {
            On.RoR2.PickupDropletController.Start += PickupDropletController_Start;
            On.RoR2.PickupDropletController.OnCollisionEnter += PickupDropletController_OnCollisionEnter;
        }

        static void PickupDropletController_Start(On.RoR2.PickupDropletController.orig_Start orig, PickupDropletController self)
        {
            orig(self);

            if (NetworkServer.active && bounceCount > 0)
            {
                ItemBounceTracker bounceTracker = self.gameObject.AddComponent<ItemBounceTracker>();
                bounceTracker.BouncesRemaining = bounceCount;
            }
        }

        static void PickupDropletController_OnCollisionEnter(On.RoR2.PickupDropletController.orig_OnCollisionEnter orig, PickupDropletController self, Collision collision)
        {
            if (self.alive && self.TryGetComponent(out ItemBounceTracker bounceTracker) && bounceTracker.TryBounce())
                return;

            orig(self, collision);
        }

        class ItemBounceTracker : MonoBehaviour
        {
            static readonly PhysicMaterial _bouncyMaterial = new PhysicMaterial("PickupBounce")
            {
                bounciness = 1f,
                bounceCombine = PhysicMaterialCombine.Maximum,
                staticFriction = 0f,
                dynamicFriction = 0f,
                frictionCombine = PhysicMaterialCombine.Minimum
            };

            readonly record struct OriginalColliderMaterialPair(Collider Collider, PhysicMaterial Material);

            public uint BouncesRemaining;

            bool _physicMaterialOverrideActive;

            OriginalColliderMaterialPair[] _originalMaterials = [];

            void Awake()
            {
                Collider[] colliders = GetComponentsInChildren<Collider>(true);
                List<OriginalColliderMaterialPair> originalMaterials = new List<OriginalColliderMaterialPair>(colliders.Length);
                foreach (Collider collider in colliders)
                {
                    if (collider.isTrigger)
                        continue;

                    originalMaterials.Add(new OriginalColliderMaterialPair(collider, collider.sharedMaterial));
                    collider.sharedMaterial = _bouncyMaterial;
                }

                _originalMaterials = originalMaterials.ToArray();
                _physicMaterialOverrideActive = true;
            }

            void OnDestroy()
            {
                if (_physicMaterialOverrideActive)
                {
                    _physicMaterialOverrideActive = false;

                    foreach (OriginalColliderMaterialPair originalMaterialPair in _originalMaterials)
                    {
                        if (originalMaterialPair.Collider)
                        {
                            originalMaterialPair.Collider.sharedMaterial = originalMaterialPair.Material;
                        }
                    }
                }
            }

            public bool TryBounce()
            {
                if (BouncesRemaining <= 0)
                {
                    Destroy(this);
                    return false;
                }

                BouncesRemaining--;
                return true;
            }
        }
    }
}
