using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RiskOfChaos.EffectDefinitions.World;
using RiskOfChaos.EffectHandling.Controllers;
using RoR2;
using System.Reflection;

namespace RiskOfChaos.Patches.Effects.World
{
    static class GuaranteedChanceRollsHooks
    {
        static bool _isRollingTougherTimesProc;

        [SystemInitializer]
        static void Init()
        {
            On.RoR2.Util.CheckRoll_float_float_CharacterMaster += checkGuaranteedRoll;

            IL.RoR2.HealthComponent.TakeDamageProcess += HealthComponent_TakeDamageProcess;
        }

        static bool checkGuaranteedRoll(On.RoR2.Util.orig_CheckRoll_float_float_CharacterMaster orig, float percentChance, float luck, CharacterMaster effectOriginMaster)
        {
            return orig(percentChance, luck, effectOriginMaster) || (percentChance > 0f &&
                                                                     !_isRollingTougherTimesProc &&
                                                                     ChaosEffectTracker.Instance &&
                                                                     ChaosEffectTracker.Instance.IsTimedEffectActive(GuaranteedChanceRolls.EffectInfo));
        }

        static void HealthComponent_TakeDamageProcess(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            if (c.TryFindNext(out ILCursor[] foundCursors,
                              x => x.MatchLdfld<HealthComponent.ItemCounts>(nameof(HealthComponent.ItemCounts.bear)),
                              x => x.MatchCallOrCallvirt(SymbolExtensions.GetMethodInfo(() => Util.CheckRoll(default, default, default)))))
            {
                ILCursor cursor = foundCursors[1];

                FieldInfo isRollingTougherTimesProc = AccessTools.DeclaredField(typeof(GuaranteedChanceRollsHooks), nameof(_isRollingTougherTimesProc));

                cursor.Emit(OpCodes.Ldc_I4_1);
                cursor.Emit(OpCodes.Stsfld, isRollingTougherTimesProc);

                cursor.Index++;

                cursor.Emit(OpCodes.Ldc_I4_0);
                cursor.Emit(OpCodes.Stsfld, isRollingTougherTimesProc);
            }
            else
            {
                Log.Error("Failed to find Tougher Times patch location");
            }
        }
    }
}
