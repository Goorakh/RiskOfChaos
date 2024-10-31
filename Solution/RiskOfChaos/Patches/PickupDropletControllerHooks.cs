using RoR2;

namespace RiskOfChaos.Patches
{
    static class PickupDropletControllerHooks
    {
        public delegate void ModifyCreatePickupDelegate(ref GenericPickupController.CreatePickupInfo pickupInfo);
        public static event ModifyCreatePickupDelegate ModifyCreatePickup;

        [SystemInitializer]
        static void Init()
        {
            On.RoR2.PickupDropletController.CreatePickup += PickupDropletController_CreatePickup;
        }

        static void PickupDropletController_CreatePickup(On.RoR2.PickupDropletController.orig_CreatePickup orig, PickupDropletController self)
        {
            ModifyCreatePickup?.Invoke(ref self.createPickupInfo);

            orig(self);
        }
    }
}
