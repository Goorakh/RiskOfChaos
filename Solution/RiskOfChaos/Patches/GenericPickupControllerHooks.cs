using RoR2;

namespace RiskOfChaos.Patches
{
    static class GenericPickupControllerHooks
    {
        public delegate void GenericPickupControllerEventDelegate(GenericPickupController genericPickupController);
        public static event GenericPickupControllerEventDelegate OnGenericPickupControllerStartGlobal;

        [SystemInitializer]
        static void Init()
        {
            On.RoR2.GenericPickupController.Start += GenericPickupController_Start;
        }

        static void GenericPickupController_Start(On.RoR2.GenericPickupController.orig_Start orig, GenericPickupController self)
        {
            orig(self);
            OnGenericPickupControllerStartGlobal?.Invoke(self);
        }
    }
}
