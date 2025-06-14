using HG;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.Trackers
{
    public sealed class OptionChestBehaviorTracker : MonoBehaviour
    {
        [SystemInitializer]
        static void Init()
        {
            On.RoR2.OptionChestBehavior.Awake += OptionChestBehavior_Awake;
        }

        static void OptionChestBehavior_Awake(On.RoR2.OptionChestBehavior.orig_Awake orig, OptionChestBehavior self)
        {
            orig(self);

            OptionChestBehaviorTracker tracker = self.gameObject.EnsureComponent<OptionChestBehaviorTracker>();
            tracker.OptionChestBehavior = self;
        }

        public OptionChestBehavior OptionChestBehavior { get; private set; }

        void OnEnable()
        {
            InstanceTracker.Add(this);
        }

        void OnDisable()
        {
            InstanceTracker.Remove(this);
        }
    }
}
