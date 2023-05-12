using HarmonyLib;
using MonoMod.Cil;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RoR2;
using System;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosEffect("guaranteed_chance_rolls", DefaultSelectionWeight = 0.9f, EffectStageActivationCountHardCap = 1, IsNetworked = true)]
    [ChaosTimedEffect(TimedEffectType.UntilStageEnd)]
    [EffectConfigBackwardsCompatibility("Effect: Guaranteed Chance Effects (Lasts 1 stage)")]
    public sealed class GuaranteedChanceRolls : TimedEffect
    {
        [InitEffectInfo]
        static readonly ChaosEffectInfo _effectInfo;

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

#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                const string ITEM_COUNTS_BEAR_FIELD_NAME = nameof(HealthComponent.ItemCounts.bear);
#pragma warning restore Publicizer001 // Accessing a member that was not originally public

                if (c.TryFindNext(out ILCursor[] foundCursors,
                                  x => x.MatchLdfld<HealthComponent.ItemCounts>(ITEM_COUNTS_BEAR_FIELD_NAME),
                                  x => x.MatchCallOrCallvirt(SymbolExtensions.GetMethodInfo(() => Util.CheckRoll(default, default, default)))))
                {
                    ILCursor cursor = foundCursors[1];

                    cursor.EmitDelegate<Action>(() => _tempDisablePatchCount++);
                    cursor.Index++;
                    cursor.EmitDelegate<Action>(() => _tempDisablePatchCount--);
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
