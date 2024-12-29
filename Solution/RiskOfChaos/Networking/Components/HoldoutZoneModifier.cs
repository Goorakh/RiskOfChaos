using RiskOfChaos.Patches;
using RoR2;
using UnityEngine.Networking;

namespace RiskOfChaos.Networking.Components
{
    public class HoldoutZoneModifier : NetworkBehaviour
    {
        [SystemInitializer]
        static void Init()
        {
            HoldoutZoneControllerEvents.OnHoldoutZoneControllerAwakeGlobal += onHoldoutZoneControllerAwakeGlobal;
        }

        static void onHoldoutZoneControllerAwakeGlobal(HoldoutZoneController holdoutZoneController)
        {
            HoldoutZoneModifier zoneModifier = holdoutZoneController.gameObject.AddComponent<HoldoutZoneModifier>();
            zoneModifier.HoldoutZoneController = holdoutZoneController;
        }

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
        public float RadiusMultiplier = 1f;

        [SyncVar]
        public float ChargeRateMultiplier = 1f;

        void subscribe(HoldoutZoneController zoneController)
        {
            zoneController.calcRadius += calcRadius;
            zoneController.calcChargeRate += calcChargeRate;
            
            if (zoneController.minimumRadius == 0f)
            {
                // Make sure all holdout zones have a minimum radius to avoid softlocks
                zoneController.minimumRadius = 5f;
            }
        }

        void unsubscribe(HoldoutZoneController zoneController)
        {
            zoneController.calcRadius -= calcRadius;
            zoneController.calcChargeRate -= calcChargeRate;
        }

        void OnEnable()
        {
            InstanceTracker.Add(this);

            if (HoldoutZoneController)
            {
                subscribe(HoldoutZoneController);
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
