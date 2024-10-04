using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RoR2;
using System.Reflection;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosTimedEffect("swap_health_shield", 60f, AllowDuplicates = false)]
    public sealed class SwapHealthShield : TimedEffect
    {
        [InitEffectInfo]
        public static new readonly TimedEffectInfo EffectInfo;

        static bool _appliedPatches;
        static void tryApplyPatches()
        {
            if (_appliedPatches)
                return;

            IL.RoR2.CharacterBody.RecalculateStats += il =>
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

                c.Prev = maxSetInstruction;

                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate(trySwapHealthShield);
            };

            _appliedPatches = true;
        }

        static void trySwapHealthShield(CharacterBody body)
        {
            if (!ChaosEffectTracker.Instance || !ChaosEffectTracker.Instance.IsTimedEffectActive(EffectInfo))
                return;

            float prevMaxHealth = body.maxHealth;
            float maxShield = Mathf.Max(0f, prevMaxHealth);

            float prevMaxShield = body.maxShield;
            float maxHealth = Mathf.Max(0f, prevMaxShield);

            if (maxHealth <= 1f)
            {
                maxShield -= Mathf.Clamp01(1f - maxHealth);
                maxHealth = 1f;
            }

            if (maxShield <= 1f)
            {
                maxHealth += maxShield;
                maxShield = 0f;
            }

            body.maxHealth = Mathf.Max(1f, maxHealth);
            body.maxShield = Mathf.Max(0f, maxShield);
        }

        public override void OnStart()
        {
            tryApplyPatches();

            foreach (CharacterBody body in CharacterBody.readOnlyInstancesList)
            {
                body.MarkAllStatsDirty();
            }
        }

        public override void OnEnd()
        {
            foreach (CharacterBody body in CharacterBody.readOnlyInstancesList)
            {
                body.MarkAllStatsDirty();
            }
        }
    }
}
