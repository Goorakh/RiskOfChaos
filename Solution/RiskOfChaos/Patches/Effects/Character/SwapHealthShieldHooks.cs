using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RiskOfChaos.EffectDefinitions.Character;
using RoR2;
using System.Reflection;

namespace RiskOfChaos.Patches.Effects.Character
{
    static class SwapHealthShieldHooks
    {
        [SystemInitializer]
        static void Init()
        {
            IL.RoR2.CharacterBody.RecalculateStats += CharacterBody_RecalculateStats;
        }

        static void CharacterBody_RecalculateStats(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            Instruction findLastCall(MethodInfo m)
            {
                c.Index = c.Instrs.Count - 1;

                Instruction lastCallInstruction = null;
                if (c.TryGotoPrev(MoveType.Before,
                                  x => x.MatchCallOrCallvirt(m)))
                {
                    lastCallInstruction = c.Next;
                }

                c.Index = 0;

                return lastCallInstruction;
            }

            int getIndex(Instruction instruction)
            {
                return instruction != null ? c.Instrs.IndexOf(instruction) : -1;
            }

            Instruction setMaxHealthCall = findLastCall(AccessTools.DeclaredPropertySetter(typeof(CharacterBody), nameof(CharacterBody.maxHealth)));
            Instruction setMaxShieldCall = findLastCall(AccessTools.DeclaredPropertySetter(typeof(CharacterBody), nameof(CharacterBody.maxShield)));

            int setMaxHealthCallIndex = getIndex(setMaxHealthCall);
            int setMaxShieldCallIndex = getIndex(setMaxShieldCall);

            Instruction maxSetInstruction;
            if (setMaxShieldCallIndex > setMaxHealthCallIndex)
            {
                maxSetInstruction = setMaxShieldCall;
            }
            else
            {
                maxSetInstruction = setMaxHealthCall;
            }

            if (maxSetInstruction == null)
            {
                Log.Error("Failed to find patch location");
                return;
            }

            c.Goto(maxSetInstruction, MoveType.After);

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(SwapHealthShield.TryApplyStatChanges);
        }
    }
}
