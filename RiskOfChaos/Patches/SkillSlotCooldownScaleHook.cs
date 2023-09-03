using Mono.Cecil.Cil;
using MonoMod.Cil;
using RiskOfChaos.ModifierController.SkillSlots;
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

        static float getCooldownScale(SkillSlot skillSlot)
        {
            if (skillSlot < 0 || skillSlot >= (SkillSlot)SkillSlotModificationManager.SKILL_SLOT_COUNT)
                return 1f;

            if (!SkillSlotModificationManager.Instance)
                return 1f;

            return SkillSlotModificationManager.Instance.NetworkSkillSlotCooldownScales[(int)skillSlot];
        }

        static float getCooldownScale(GenericSkill skill)
        {
            if (skill)
            {
                CharacterBody body = skill.characterBody;
                if (body)
                {
                    SkillLocator skillLocator = body.skillLocator;
                    if (skillLocator)
                    {
                        return getCooldownScale(skillLocator.FindSkillSlot(skill));
                    }
                }
            }

            return 1f;
        }

        static float GenericSkill_CalculateFinalRechargeInterval(On.RoR2.GenericSkill.orig_CalculateFinalRechargeInterval orig, GenericSkill self)
        {
            return orig(self) * getCooldownScale(self);
        }
    }
}
