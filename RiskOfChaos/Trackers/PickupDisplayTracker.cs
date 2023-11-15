using RoR2;
using UnityEngine;

namespace RiskOfChaos.Trackers
{
    public sealed class PickupDisplayTracker : MonoBehaviour
    {
        [SystemInitializer]
        static void Init()
        {
            On.RoR2.PickupDisplay.Start += (orig, self) =>
            {
                orig(self);

                PickupDisplayTracker tracker = self.gameObject.AddComponent<PickupDisplayTracker>();
                tracker.PickupDisplay = self;
            };
        }

        public PickupDisplay PickupDisplay { get; private set; }

        void OnEnable()
        {
            InstanceTracker.Add(this);
        }
        
        void FixedUpdate()
        {
            if (!PickupDisplay)
            {
                Destroy(this);
            }
        }

        void OnDisable()
        {
            InstanceTracker.Remove(this);
        }
    }
}
