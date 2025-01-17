using RoR2;

namespace RiskOfChaos.Patches
{
    static class PurchaseInteractionHooks
    {
        public delegate void PurchaseInteractionDelegate(PurchaseInteraction purchaseInteraction);

        public static event PurchaseInteractionDelegate OnPurchaseInteractionAwakeGlobal;

        public static event PurchaseInteractionDelegate OnPurchaseInteractionStartGlobal;

        [SystemInitializer]
        static void Init()
        {
            On.RoR2.PurchaseInteraction.Awake += PurchaseInteraction_Awake;
            On.RoR2.PurchaseInteraction.Start += PurchaseInteraction_Start;
        }

        static void PurchaseInteraction_Awake(On.RoR2.PurchaseInteraction.orig_Awake orig, PurchaseInteraction self)
        {
            OnPurchaseInteractionAwakeGlobal?.Invoke(self);
            orig(self);
        }

        static void PurchaseInteraction_Start(On.RoR2.PurchaseInteraction.orig_Start orig, PurchaseInteraction self)
        {
            orig(self);
            OnPurchaseInteractionStartGlobal?.Invoke(self);
        }
    }
}
