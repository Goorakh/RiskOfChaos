using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RoR2;
using System.Reflection;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosTimedEffect("guaranteed_chance_rolls", 60f, AllowDuplicates = false, DefaultSelectionWeight = 0.9f, IsNetworked = true)]
    [EffectConfigBackwardsCompatibility("Effect: Guaranteed Chance Effects (Lasts 1 stage)")]
    public sealed class GuaranteedChanceRolls : TimedEffect
    {
        [InitEffectInfo]
        static readonly TimedEffectInfo _effectInfo;

        static bool _hasAppliedPatches;
        static bool _isRollingTougherTimesProc;
        static void tryApplyPatches()
        {
            if (_hasAppliedPatches)
                return;

            _hasAppliedPatches = true;

            On.RoR2.Util.CheckRoll_float_float_CharacterMaster += checkGuaranteedRoll;
            static bool checkGuaranteedRoll(On.RoR2.Util.orig_CheckRoll_float_float_CharacterMaster orig, float percentChance, float luck, CharacterMaster effectOriginMaster)
            {
                return orig(percentChance, luck, effectOriginMaster) || (percentChance > 0f &&
                                                                         !_isRollingTougherTimesProc &&
                                                                         ChaosEffectTracker.Instance &&
                                                                         ChaosEffectTracker.Instance.IsTimedEffectActive(_effectInfo));
            }

            IL.RoR2.HealthComponent.TakeDamageProcess += il =>
            {
                ILCursor c = new ILCursor(il);

                if (c.TryFindNext(out ILCursor[] foundCursors,
                                  x => x.MatchLdfld<HealthComponent.ItemCounts>(nameof(HealthComponent.ItemCounts.bear)),
                                  x => x.MatchCallOrCallvirt(SymbolExtensions.GetMethodInfo(() => Util.CheckRoll(default, default, default)))))
                {
                    ILCursor cursor = foundCursors[1];

                    FieldInfo isRollingTougherTimesProc = AccessTools.DeclaredField(typeof(GuaranteedChanceRolls), nameof(_isRollingTougherTimesProc));

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
            };
        }

        public override void OnStart()
        {
            tryApplyPatches();
            _isRollingTougherTimesProc = false;
        }

        public override void OnEnd()
        {
            _isRollingTougherTimesProc = false;
        }
    }
}
