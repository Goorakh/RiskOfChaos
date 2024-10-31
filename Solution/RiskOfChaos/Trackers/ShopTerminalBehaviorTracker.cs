using RiskOfChaos.Utilities.Extensions;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.Trackers
{
    public sealed class ShopTerminalBehaviorTracker : MonoBehaviour
    {
        [SystemInitializer]
        static void Init()
        {
            On.RoR2.ShopTerminalBehavior.Start += ShopTerminalBehavior_Start;
        }

        static void ShopTerminalBehavior_Start(On.RoR2.ShopTerminalBehavior.orig_Start orig, ShopTerminalBehavior self)
        {
            orig(self);

            ShopTerminalBehaviorTracker tracker = self.gameObject.EnsureComponent<ShopTerminalBehaviorTracker>();
            tracker.ShopTerminalBehavior = self;
        }

        public ShopTerminalBehavior ShopTerminalBehavior { get; private set; }

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
