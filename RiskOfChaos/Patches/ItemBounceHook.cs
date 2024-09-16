using RiskOfChaos.ModifierController.Pickups;
using RoR2;
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
            if (self.alive && self.TryGetComponent(out ItemBounceTracker bounceTracker) && bounceTracker.TryBounce(collision))
            {
                return;
            }

            orig(self, collision);
        }

        class ItemBounceTracker : MonoBehaviour
        {
            static readonly float _bounceVelocityMultiplier = 1f;

            public uint BouncesRemaining;

            float _timeSinceLastBounce;

            Vector3 _lastNonZeroVelocity;
            Rigidbody _rigidbody;

            void Awake()
            {
                _rigidbody = GetComponent<Rigidbody>();
            }

            void FixedUpdate()
            {
                _timeSinceLastBounce += Time.fixedDeltaTime;
                if (_timeSinceLastBounce > 7.5f)
                {
#if DEBUG
                    Log.Debug($"Pickup bounce timeout reached for {name}");
#endif

                    BouncesRemaining = 0;
                    Destroy(this);

                    // IMPORTANT: Always leave this part as the last part to be executed before returning,
                    // OnCollisionEnter could potentially raise an exception if null argument isn't accounted for
                    if (TryGetComponent(out PickupDropletController pickupDropletController))
                    {
                        pickupDropletController.OnCollisionEnter(null);
                    }

                    return;
                }

                Vector3 velocity = _rigidbody.velocity;
                if (velocity.sqrMagnitude > 0f)
                {
                    _lastNonZeroVelocity = velocity;
                }
            }

            public bool TryBounce(Collision collision)
            {
                if (BouncesRemaining <= 0)
                {
                    Destroy(this);
                    return false;
                }

                if (collision is null || collision.contactCount <= 0)
                    return false;

                Vector3 normal = collision.GetContact(0).normal;

                Vector3 newDirection = Vector3.Reflect(_lastNonZeroVelocity.normalized, normal);

                _rigidbody.velocity = newDirection * (_lastNonZeroVelocity.magnitude * _bounceVelocityMultiplier);

                _timeSinceLastBounce = 0f;

                BouncesRemaining--;

                if (BouncesRemaining <= 0)
                {
                    Destroy(this);
                }

                return true;
            }
        }
    }
}
