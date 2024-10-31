using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System;
using UnityEngine.Networking;

namespace RiskOfChaos.Networking.Components
{
    public class HoldoutZoneModifier : NetworkBehaviour
    {
        [SystemInitializer]
        static void Init()
        {
            On.RoR2.HoldoutZoneController.Awake += (orig, self) =>
            {
                orig(self);

                HoldoutZoneModifier zoneModifier = self.gameObject.EnsureComponent<HoldoutZoneModifier>();
                zoneModifier.HoldoutZoneController = self;

                if (self.minimumRadius == 0f)
                {
                    // Make sure all holdout zones have a minimum radius to avoid softlocks
                    self.minimumRadius = 5f;
                }
            };
        }

        public static event Action<HoldoutZoneModifier> OnHoldoutZoneEnabled;

        HoldoutZoneController _holdoutZoneController;
        public HoldoutZoneController HoldoutZoneController
        {
            get
            {
                return _holdoutZoneController;
            }
            private set
            {
                if (_holdoutZoneController == value)
                    return;

                if (_holdoutZoneController)
                    unsubscribe(_holdoutZoneController);

                _holdoutZoneController = value;

                if (_holdoutZoneController && enabled)
                    subscribe(_holdoutZoneController);
            }
        }

        [SyncVar]
        public float RadiusMultiplier;

        [SyncVar]
        public float ChargeRateMultiplier;

        void subscribe(HoldoutZoneController zoneController)
        {
            zoneController.calcRadius += calcRadius;
            zoneController.calcChargeRate += calcChargeRate;
        }

        void unsubscribe(HoldoutZoneController zoneController)
        {
            zoneController.calcRadius -= calcRadius;
            zoneController.calcChargeRate -= calcChargeRate;
        }

        void Start()
        {
            if (HoldoutZoneController)
            {
                OnHoldoutZoneEnabled?.Invoke(this);
            }
        }

        void OnEnable()
        {
            InstanceTracker.Add(this);

            if (HoldoutZoneController)
            {
                subscribe(HoldoutZoneController);

                OnHoldoutZoneEnabled?.Invoke(this);
            }
        }

        void OnDisable()
        {
            InstanceTracker.Remove(this);

            if (HoldoutZoneController)
            {
                unsubscribe(HoldoutZoneController);
            }
        }

        void calcRadius(ref float radius)
        {
            radius *= RadiusMultiplier;
        }

        void calcChargeRate(ref float rate)
        {
            rate *= ChargeRateMultiplier;
        }
    }
}
