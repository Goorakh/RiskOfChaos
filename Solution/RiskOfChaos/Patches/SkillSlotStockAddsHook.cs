using HarmonyLib;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RiskOfChaos.ModificationController.SkillSlots;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.Skills;
using UnityEngine;

namespace RiskOfChaos.Patches
{
    static class SkillSlotStockAddsHook
    {
        [SystemInitializer]
        static void Init()
        {
            IL.RoR2.GenericSkill.RecalculateMaxStock += GenericSkill_RecalculateMaxStock;

            // Fix ReloadSkillDef reading from the SkillDef max stock instead of the skill slot's max stock
            IL.RoR2.Skills.ReloadSkillDef.OnFixedUpdate += ReloadSkillDef_OnFixedUpdate;
        }

        static void GenericSkill_RecalculateMaxStock(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            if (!c.TryGotoNext(MoveType.Before,
                               x => x.MatchCallOrCallvirt(AccessTools.DeclaredPropertySetter(typeof(GenericSkill), nameof(GenericSkill.maxStock)))))
            {
                Log.Error("Failed to find maxStock patch location");
                return;
            }

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(modifyMaxStock);
            static int modifyMaxStock(int maxStock, GenericSkill instance)
            {
                if (SkillSlotModificationManager.Instance)
                {
                    maxStock = Mathf.Max(1, maxStock + SkillSlotModificationManager.Instance.StockAdd);
                }

                return maxStock;
            }
        }

        static void ReloadSkillDef_OnFixedUpdate(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            if (!il.Method.TryFindParameter<GenericSkill>(out ParameterDefinition skillSlotParameter))
            {
                Log.Error("Failed to find skill slot parameter");
                return;
            }

            if (!c.TryGotoNext(MoveType.After,
                               x => x.MatchCallOrCallvirt<SkillDef>(nameof(SkillDef.GetMaxStock))))
            {
                Log.Error("Failed to find ReloadSkillDef stock fix patch location");
                return;
            }

            // Pop SkillDef max stock
            c.Emit(OpCodes.Pop);

            // arg1: GenericSkill skillSlot
            c.Emit(OpCodes.Ldarg, skillSlotParameter);
            c.Emit(OpCodes.Callvirt, AccessTools.DeclaredPropertyGetter(typeof(GenericSkill), nameof(GenericSkill.maxStock)));
        }
    }
}
