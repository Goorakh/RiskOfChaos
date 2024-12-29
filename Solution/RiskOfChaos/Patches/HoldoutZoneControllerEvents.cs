using RoR2;

namespace RiskOfChaos.Patches
{
    static class HoldoutZoneControllerEvents
    {
        public delegate void HoldoutZoneControllerEventHandler(HoldoutZoneController holdoutZoneController);

        public static event HoldoutZoneControllerEventHandler OnHoldoutZoneControllerAwakeGlobal;

        [SystemInitializer]
        static void Init()
        {
            On.RoR2.HoldoutZoneController.Awake += HoldoutZoneController_Awake;
        }

        static void HoldoutZoneController_Awake(On.RoR2.HoldoutZoneController.orig_Awake orig, HoldoutZoneController self)
        {
            orig(self);

            OnHoldoutZoneControllerAwakeGlobal?.Invoke(self);
        }
    }
}
