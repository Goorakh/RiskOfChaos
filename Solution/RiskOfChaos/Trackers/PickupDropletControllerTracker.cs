using RiskOfChaos.Utilities.Extensions;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.Trackers
{
    public sealed class PickupDropletControllerTracker : MonoBehaviour
    {
        [SystemInitializer]
        static void Init()
        {
            On.RoR2.PickupDropletController.Start += PickupDropletController_Start;
        }

        static void PickupDropletController_Start(On.RoR2.PickupDropletController.orig_Start orig, PickupDropletController self)
        {
            orig(self);

            PickupDropletControllerTracker tracker = self.gameObject.EnsureComponent<PickupDropletControllerTracker>();
            tracker.PickupDropletController = self;
        }

        public delegate void PickupDropletControllerTrackerEventDelegate(PickupDropletController pickupDropletController);
        public static event PickupDropletControllerTrackerEventDelegate OnPickupDropletControllerStartGlobal;

        public PickupDropletController PickupDropletController { get; private set; }

        void Start()
        {
            InstanceTracker.Add(this);
            OnPickupDropletControllerStartGlobal?.Invoke(PickupDropletController);
        }

        void OnDestroy()
        {
            InstanceTracker.Remove(this);
        }
    }
}
