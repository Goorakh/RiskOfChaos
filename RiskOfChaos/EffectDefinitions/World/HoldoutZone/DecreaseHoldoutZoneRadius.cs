using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfOptions.OptionConfigs;

namespace RiskOfChaos.EffectDefinitions.World.HoldoutZone
{
    [ChaosTimedEffect("decrease_holdout_zone_radius", TimedEffectType.UntilStageEnd, ConfigName = "Decrease Teleporter Zone Radius")]
    public sealed class DecreaseHoldoutZoneRadius : GenericHoldoutZoneModifierEffect
    {
        [EffectConfig]
        static readonly ConfigHolder<float> _radiusDecrease =
            ConfigFactory<float>.CreateConfig("Radius Decrease", 0.5f)
                                .Description("Percentage decrease of teleporter radius")
                                .OptionConfig(new StepSliderConfig
                                {
                                    formatString = "-{0:P0}",
                                    increment = 0.01f,
                                    min = 0f,
                                    max = 1f
                                })
                                .ValueConstrictor(CommonValueConstrictors.Clamped01Float)
                                .Build();

        [EffectNameFormatArgs]
        static object[] GetEffectNameFormatArgs()
        {
            return new object[]
            {
                _radiusDecrease.Value
            };
        }

        protected override void modifyRadius(ref float radius)
        {
            base.modifyRadius(ref radius);
            radius *= 1f - _radiusDecrease.Value;
        }
    }
}
