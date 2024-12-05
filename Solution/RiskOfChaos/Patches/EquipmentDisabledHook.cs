using RoR2;

namespace RiskOfChaos.Patches
{
    static class EquipmentDisabledHook
    {
        public delegate void OverrideEquipmentDisabledDelegate(Inventory inventory, ref bool isDisabled);
        public static event OverrideEquipmentDisabledDelegate OverrideEquipmentDisabled;

        [SystemInitializer]
        static void Init()
        {
            On.RoR2.Inventory.GetEquipmentDisabled += Inventory_GetEquipmentDisabled;
        }

        static bool Inventory_GetEquipmentDisabled(On.RoR2.Inventory.orig_GetEquipmentDisabled orig, Inventory self)
        {
            bool isDisabled = orig(self);
            OverrideEquipmentDisabled?.Invoke(self, ref isDisabled);
            return isDisabled;
        }
    }
}
