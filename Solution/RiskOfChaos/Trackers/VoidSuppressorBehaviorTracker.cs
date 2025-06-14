using HG;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.Trackers
{
    public sealed class VoidSuppressorBehaviorTracker : MonoBehaviour
    {
        [SystemInitializer]
        static void Init()
        {
            On.RoR2.VoidSuppressorBehavior.Awake += VoidSuppressorBehavior_Awake;
        }

        static void VoidSuppressorBehavior_Awake(On.RoR2.VoidSuppressorBehavior.orig_Awake orig, VoidSuppressorBehavior self)
        {
            orig(self);

            VoidSuppressorBehaviorTracker tracker = self.gameObject.EnsureComponent<VoidSuppressorBehaviorTracker>();
            tracker.VoidSuppressorBehavior = self;
        }

        public VoidSuppressorBehavior VoidSuppressorBehavior { get; private set; }

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
