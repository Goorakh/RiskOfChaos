using HarmonyLib;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;
using R2API;
using RoR2;
using System;

namespace RiskOfChaos.Content
{
    public static class CustomDOTs
    {
        public static DotController.DotIndex PercentHealthDotIndex { get; private set; } = DotController.DotIndex.None;

        [SystemInitializer(typeof(DotController))]
        static void Init()
        {
            const float PERCENT_HEALTH_DOT_TICKS_PER_SECOND = 7f;
            PercentHealthDotIndex = DotAPI.RegisterDotDef(new DotController.DotDef
            {
                damageCoefficient = 1f / PERCENT_HEALTH_DOT_TICKS_PER_SECOND,
                interval = 1f / PERCENT_HEALTH_DOT_TICKS_PER_SECOND,
                damageColorIndex = DamageColorIndex.Fragile,
                associatedBuff = RoR2Content.Buffs.OnFire
            });

            IL.RoR2.DotController.AddDot += il =>
            {
                ILCursor c = new ILCursor(il);

                bool tryFindParameterIndex(Type parameterType, string name, out int parameterIndex)
                {
                    for (int i = 0; i < il.Method.Parameters.Count; i++)
                    {
                        ParameterDefinition parameter = il.Method.Parameters[i];
                        if (string.Equals(parameter.Name, name) && parameter.ParameterType.Is(parameterType))
                        {
                            if (il.Method.IsStatic)
                            {
                                parameterIndex = i;
                            }
                            else
                            {
                                parameterIndex = i + 1;
                            }

                            return true;
                        }
                    }

                    parameterIndex = -1;
                    return false;
                }

                if (!tryFindParameterIndex(typeof(DotController.DotIndex), "dotIndex", out int dotIndexParameterIndex))
                {
                    Log.Error("Unable to find dotIndex argument");
                    return;
                }

                if (!tryFindParameterIndex(typeof(float), "damageMultiplier", out int damageMultiplierParameterIndex))
                {
                    Log.Error("Unable to find damageMultiplier argument");
                    return;
                }

                int dotStackLocalIndex = -1;
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                if (c.TryGotoNext(x => x.MatchLdsfld<DotController>(nameof(DotController.dotStackPool)),
                                  x => x.MatchCallOrCallvirt(out _),
                                  x => x.MatchStloc(out dotStackLocalIndex)))
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
                {
                    if (c.TryGotoNext(MoveType.Before, x => x.MatchLdarg(out _), x => x.MatchSwitch(out _)))
                    {
                        c.Emit(OpCodes.Ldarg_0);
                        c.Emit(OpCodes.Ldarg, dotIndexParameterIndex);
                        c.Emit(OpCodes.Ldarg, damageMultiplierParameterIndex);
                        c.Emit(OpCodes.Ldloc, dotStackLocalIndex);

                        c.EmitDelegate((DotController instance, DotController.DotIndex dotIndex, float damageMultipler, DotController.DotStack dotStack) =>
                        {
                            if (PercentHealthDotIndex != DotController.DotIndex.None && dotIndex == PercentHealthDotIndex)
                            {
                                DotController.DotDef dotDef = DotController.GetDotDef(dotIndex);

#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                                dotStack.damage = (instance.victimHealthComponent.fullCombinedHealth / 100f) * damageMultipler * dotDef.damageCoefficient;
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
                            }
                        });
                    }
                    else
                    {
                        Log.Error("Failed to find dotIndex switch");
                    }
                }
                else
                {
                    Log.Error("Failed to find dotStack local index");
                }
            };
        }
    }
}
