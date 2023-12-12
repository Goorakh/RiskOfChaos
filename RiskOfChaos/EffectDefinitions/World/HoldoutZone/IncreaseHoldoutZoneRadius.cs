using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.Formatting;
using RiskOfOptions.OptionConfigs;
using RoR2;

namespace RiskOfChaos.EffectDefinitions.World.HoldoutZone
{
    [ChaosTimedEffect("increase_holdout_zone_radius", TimedEffectType.UntilStageEnd, ConfigName = "Increase Teleporter Zone Radius")]
    public sealed class IncreaseHoldoutZoneRadius : GenericHoldoutZoneModifierEffect
    {
        [EffectConfig]
        static readonly ConfigHolder<float> _radiusIncrease =
            ConfigFactory<float>.CreateConfig("Radius Increase", 0.5f)
                                .Description("Percentage increase of teleporter radius")
                                .OptionConfig(new StepSliderConfig
                                {
                                    formatString = "+{0:P0}",
                                    increment = 0.01f,
                                    min = 0f,
                                    max = 2f
                                })
                                .ValueConstrictor(CommonValueConstrictors.GreaterThanOrEqualTo(0f))
                                .Build();

        [GetEffectNameFormatter]
        static EffectNameFormatter GetNameFormatter()
        {
            return new EffectNameFormatter_GenericFloat(_radiusIncrease.Value) { ValueFormat = "P0" };
        }

        protected override void modifyRadius(HoldoutZoneController controller, ref float radius)
        {
            base.modifyRadius(controller, ref radius);
            radius *= 1f + _radiusIncrease.Value;
        }
    }
}
