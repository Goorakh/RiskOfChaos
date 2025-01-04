using RiskOfChaos.Components;
using RoR2;
using RoR2.UI;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfChaos.Patches
{
    static class CustomPingBehaviorHooks
    {
        static readonly List<ICustomPingBehavior> _customPingBehaviorComponentBuffer = [];

        [SystemInitializer]
        static void Init()
        {
            On.RoR2.UI.PingIndicator.RebuildPing += PingIndicator_RebuildPing;
            On.RoR2.UI.PingIndicator.DestroyPing += PingIndicator_DestroyPing;
            On.RoR2.PingerController.SetCurrentPing += PingerController_SetCurrentPing;
        }

        static void onPingAdded(PingIndicator pingIndicator, GameObject target)
        {
            if (!pingIndicator || !target)
                return;
            
            target.GetComponents(_customPingBehaviorComponentBuffer);
            foreach (ICustomPingBehavior customPingBehavior in _customPingBehaviorComponentBuffer)
            {
                customPingBehavior.OnPingAdded(pingIndicator);
            }
        }

        static void onPingRemoved(PingIndicator pingIndicator, GameObject target)
        {
            if (!pingIndicator || !target)
                return;

            target.GetComponents(_customPingBehaviorComponentBuffer);
            foreach (ICustomPingBehavior customPingBehavior in _customPingBehaviorComponentBuffer)
            {
                customPingBehavior.OnPingRemoved(pingIndicator);
            }
        }

        static void PingIndicator_RebuildPing(On.RoR2.UI.PingIndicator.orig_RebuildPing orig, PingIndicator self)
        {
            orig(self);

            if (self)
            {
                onPingAdded(self, self.pingTarget);
            }
        }

        static void PingIndicator_DestroyPing(On.RoR2.UI.PingIndicator.orig_DestroyPing orig, PingIndicator self)
        {
            if (self)
            {
                onPingRemoved(self, self.pingTarget);
            }

            orig(self);
        }

        static void PingerController_SetCurrentPing(On.RoR2.PingerController.orig_SetCurrentPing orig, PingerController self, PingerController.PingInfo newPingInfo)
        {
            if (self)
            {
                onPingRemoved(self.pingIndicator, self.currentPing.targetGameObject);
            }

            orig(self, newPingInfo);
        }
    }
}
