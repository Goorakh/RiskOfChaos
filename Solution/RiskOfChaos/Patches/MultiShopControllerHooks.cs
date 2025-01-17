using RoR2;

namespace RiskOfChaos.Patches
{
    static class MultiShopControllerHooks
    {
        public delegate void MultiShopControllerDelegate(MultiShopController multiShopController);

        public static event MultiShopControllerDelegate OnMultiShopControllerStartGlobal;

        [SystemInitializer]
        static void Init()
        {
            On.RoR2.MultiShopController.Start += MultiShopController_Start;
        }

        static void MultiShopController_Start(On.RoR2.MultiShopController.orig_Start orig, MultiShopController self)
        {
            OnMultiShopControllerStartGlobal?.Invoke(self);
            orig(self);
        }
    }
}
