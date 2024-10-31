using RiskOfChaos.EffectDefinitions.Character.Equipment;
using RiskOfChaos.EffectHandling.Controllers;
using RoR2;

namespace RiskOfChaos.Patches.Effects.Character.Equipment
{
    static class NoEquipmentCooldownHooks
    {
        [SystemInitializer]
        static void Init()
        {
            On.RoR2.Inventory.CalculateEquipmentCooldownScale += Inventory_CalculateEquipmentCooldownScale;
        }

        static float Inventory_CalculateEquipmentCooldownScale(On.RoR2.Inventory.orig_CalculateEquipmentCooldownScale orig, Inventory self)
        {
            float cooldownScale = orig(self);
            if (ChaosEffectTracker.Instance)
            {
                if (ChaosEffectTracker.Instance.IsTimedEffectActive(NoEquipmentCooldown.EffectInfo))
                {
                    cooldownScale = 0f;
                }
            }

            return cooldownScale;
        }
    }
}
