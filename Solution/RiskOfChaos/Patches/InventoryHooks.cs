using RoR2;
using System;

namespace RiskOfChaos.Patches
{
    static class InventoryHooks
    {
        public delegate void OverrideEquipmentCooldownScaleDelegate(Inventory inventory, ref float cooldownScale);
        public static event OverrideEquipmentCooldownScaleDelegate OverrideEquipmentCooldownScale;

        [SystemInitializer]
        static void Init()
        {
            On.RoR2.Inventory.CalculateEquipmentCooldownScale += Inventory_CalculateEquipmentCooldownScale;
        }

        static float Inventory_CalculateEquipmentCooldownScale(On.RoR2.Inventory.orig_CalculateEquipmentCooldownScale orig, Inventory self)
        {
            float cooldownScale = orig(self);

            if (OverrideEquipmentCooldownScale != null)
            {
                float tempCooldownScale = cooldownScale;
                try
                {
                    OverrideEquipmentCooldownScale(self, ref tempCooldownScale);
                    cooldownScale = tempCooldownScale;
                }
                catch (Exception e)
                {
                    Log.Error_NoCallerPrefix(e);
                }
            }

            return cooldownScale;
        }
    }
}
