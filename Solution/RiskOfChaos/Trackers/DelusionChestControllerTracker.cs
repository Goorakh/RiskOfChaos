using RoR2;
using UnityEngine;

namespace RiskOfChaos.Trackers
{
    public sealed class DelusionChestControllerTracker : MonoBehaviour
    {
        [SystemInitializer]
        static void Init()
        {
            On.RoR2.DelusionChestController.Start += (orig, self) =>
            {
                orig(self);

                DelusionChestControllerTracker tracker = self.gameObject.AddComponent<DelusionChestControllerTracker>();
                tracker.DelusionChestController = self;
            };

            On.RoR2.GenericPickupController.AttemptGrant += (orig, self, body) =>
            {
                orig(self, body);

                if (self.chestGeneratedFrom &&
                    !DelusionChestController.isDelusionEnable &&
                    self.chestGeneratedFrom.TryGetComponent(out DelusionChestControllerTracker delusionChestTracker))
                {
                    delusionChestTracker._pendingDelusionPickupIndex = self.pickup.pickupIndex;
                }
            };
        }

        public DelusionChestController DelusionChestController { get; private set; }

        PickupIndex _pendingDelusionPickupIndex = PickupIndex.none;
        public PickupIndex TakePendingDelusionPickupIndex()
        {
            PickupIndex pickupIndex = _pendingDelusionPickupIndex;
            _pendingDelusionPickupIndex = PickupIndex.none;
            return pickupIndex;
        }

        void Awake()
        {
            InstanceTracker.Add(this);
        }

        void OnDestroy()
        {
            InstanceTracker.Remove(this);
        }
    }
}
