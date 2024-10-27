using RoR2;

namespace RiskOfChaos.Patches
{
    static class PickupPickerControllerHooks
    {
        public delegate void PickupPickerControllerEventDelegate(PickupPickerController pickupPickerController);
        public static event PickupPickerControllerEventDelegate OnPickupPickerControllerAwakeGlobal;

        [SystemInitializer]
        static void Init()
        {
            On.RoR2.PickupPickerController.Awake += PickupPickerController_Awake;
        }

        static void PickupPickerController_Awake(On.RoR2.PickupPickerController.orig_Awake orig, PickupPickerController self)
        {
            orig(self);
            OnPickupPickerControllerAwakeGlobal?.Invoke(self);
        }
    }
}
