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
        static int _tempDisablePatchCount;
        static void tryApplyPatches()
        {
            if (_hasAppliedPatches)
                return;

            On.RoR2.Util.CheckRoll_float_float_CharacterMaster += (orig, percentChance, luck, effectOriginMaster) =>
            {
                return orig(percentChance, luck, effectOriginMaster) || (percentChance > 0f &&
                                                                         _tempDisablePatchCount <= 0 &&
                                                                         TimedChaosEffectHandler.Instance &&
                                                                         TimedChaosEffectHandler.Instance.IsTimedEffectActive(_effectInfo));
            };

            IL.RoR2.HealthComponent.TakeDamage += il =>
            {
                ILCursor c = new ILCursor(il);

                if (c.TryFindNext(out ILCursor[] foundCursors,
                                  x => x.MatchLdfld<HealthComponent.ItemCounts>(nameof(HealthComponent.ItemCounts.bear)),
                                  x => x.MatchCallOrCallvirt(SymbolExtensions.GetMethodInfo(() => Util.CheckRoll(default, default, default)))))
                {
                    ILCursor cursor = foundCursors[1];

                    FieldInfo tempDisablePatchCount = AccessTools.DeclaredField(typeof(GuaranteedChanceRolls), nameof(_tempDisablePatchCount));

                    cursor.Emit(OpCodes.Ldsfld, tempDisablePatchCount);
                    cursor.Emit(OpCodes.Ldc_I4_1);
                    cursor.Emit(OpCodes.Add);
                    cursor.Emit(OpCodes.Stsfld, tempDisablePatchCount);

                    cursor.Index++;

                    cursor.Emit(OpCodes.Ldsfld, tempDisablePatchCount);
                    cursor.Emit(OpCodes.Ldc_I4_1);
                    cursor.Emit(OpCodes.Sub);
                    cursor.Emit(OpCodes.Stsfld, tempDisablePatchCount);
                }
                else
                {
                    Log.Error("Failed to find Tougher Times patch location");
                }
            };

            _hasAppliedPatches = true;
        }

        public override void OnStart()
        {
            tryApplyPatches();
            _tempDisablePatchCount = 0;
        }

        public override void OnEnd()
        {
            _tempDisablePatchCount = 0;
        }
    }
}
