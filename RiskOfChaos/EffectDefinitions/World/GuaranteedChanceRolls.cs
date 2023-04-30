using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;

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
        static void tryApplyPatches()
        {
            if (_hasAppliedPatches)
                return;

            On.RoR2.Util.CheckRoll_float_float_CharacterMaster += (orig, percentChance, luck, effectOriginMaster) =>
            {
                return orig(percentChance, luck, effectOriginMaster) || (percentChance > 0f &&
                                                                         TimedChaosEffectHandler.Instance &&
                                                                         TimedChaosEffectHandler.Instance.IsTimedEffectActive(_effectInfo));
            };

            _hasAppliedPatches = true;
        }

        public override void OnStart()
        {
            tryApplyPatches();
        }

        public override void OnEnd()
        {
        }
    }
}
