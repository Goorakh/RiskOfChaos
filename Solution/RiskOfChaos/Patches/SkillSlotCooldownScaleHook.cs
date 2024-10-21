using RiskOfChaos.ModificationController.SkillSlots;
using RoR2;

namespace RiskOfChaos.Patches
{
    static class SkillSlotCooldownScaleHook
    {
        [SystemInitializer]
        static void Init()
        {
            On.RoR2.GenericSkill.CalculateFinalRechargeInterval += GenericSkill_CalculateFinalRechargeInterval;
        }

        static float GenericSkill_CalculateFinalRechargeInterval(On.RoR2.GenericSkill.orig_CalculateFinalRechargeInterval orig, GenericSkill self)
        {
            float cooldown = orig(self);

            if (SkillSlotModificationManager.Instance)
            {
                cooldown *= SkillSlotModificationManager.Instance.CooldownMultiplier;
            }

            return cooldown;
        }
    }
}
