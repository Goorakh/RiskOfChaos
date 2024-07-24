using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RiskOfChaos.ModifierController.SkillSlots;
using RoR2;
using RoR2.Skills;
using System;

namespace RiskOfChaos.Patches
{
    static class SkillSlotStockAddsHook
    {
        [SystemInitializer]
        static void Init()
        {
            IL.RoR2.GenericSkill.RecalculateMaxStock += il =>
            {
                ILCursor c = new ILCursor(il);

                if (c.TryGotoNext(MoveType.Before,
                                  x => x.MatchCallOrCallvirt(AccessTools.DeclaredPropertySetter(typeof(GenericSkill), nameof(GenericSkill.maxStock)))))
                {
                    c.Emit(OpCodes.Ldarg_0);
                    c.EmitDelegate(modifyMaxStock);
                    static int modifyMaxStock(int maxStock, GenericSkill instance)
                    {
                        return Math.Max(1, maxStock + getStockAdds(instance));
                    }
                }
            };

            // Fix ReloadSkillDef reading from the SkillDef max stock instead of the skill slot's max stock
            IL.RoR2.Skills.ReloadSkillDef.OnFixedUpdate += il =>
            {
                ILCursor c = new ILCursor(il);

                if (c.TryGotoNext(MoveType.After,
                                  x => x.MatchCallOrCallvirt<SkillDef>(nameof(SkillDef.GetMaxStock))))
                {
                    // Pop SkillDef max stock
                    c.Emit(OpCodes.Pop);

                    // arg1: GenericSkill skillSlot
                    c.Emit(OpCodes.Ldarg_1);
                    c.Emit(OpCodes.Callvirt, AccessTools.DeclaredPropertyGetter(typeof(GenericSkill), nameof(GenericSkill.maxStock)));
                }
            };
        }

        static int getStockAdds(SkillSlot skillSlot)
        {
            if (!SkillSlotModificationManager.Instance)
                return 0;

            return SkillSlotModificationManager.Instance.GetStockAdd(skillSlot);
        }

        static int getStockAdds(GenericSkill skill)
        {
            if (skill)
            {
                CharacterBody body = skill.characterBody;
                if (body)
                {
                    SkillLocator skillLocator = body.skillLocator;
                    if (skillLocator)
                    {
                        return getStockAdds(skillLocator.FindSkillSlot(skill));
                    }
                }
            }

            return 0;
        }
    }
}
