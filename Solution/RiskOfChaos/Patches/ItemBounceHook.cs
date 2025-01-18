using RiskOfChaos.Components;
using RiskOfChaos.ModificationController.Pickups;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Patches
{
    static class ItemBounceHook
    {
        static int bounceCount
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

            public int BouncesRemaining;

            PhysicMaterialOverride _materialOverrideController;

            void Awake()
            {
                _materialOverrideController = PhysicMaterialOverride.AddOverrideMaterial(gameObject, _bouncyMaterial, 1);
            }

            void OnDestroy()
            {
                if (_materialOverrideController)
                {
                    _materialOverrideController.RemoveOverrideMaterial(_bouncyMaterial);
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
