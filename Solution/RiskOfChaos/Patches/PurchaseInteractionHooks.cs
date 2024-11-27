using RoR2;
using System;

namespace RiskOfChaos.Patches
{
    static class PurchaseInteractionHooks
    {
        public static event Action<PurchaseInteraction> OnPurchaseInteractionStartGlobal;

        [SystemInitializer]
        static void Init()
        {
            On.RoR2.PurchaseInteraction.Start += PurchaseInteraction_Start;
        }

        static void PurchaseInteraction_Start(On.RoR2.PurchaseInteraction.orig_Start orig, PurchaseInteraction self)
        {
            orig(self);
            OnPurchaseInteractionStartGlobal?.Invoke(self);
        }
    }
}
