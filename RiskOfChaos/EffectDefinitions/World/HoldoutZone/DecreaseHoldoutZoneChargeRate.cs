using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfOptions.OptionConfigs;

namespace RiskOfChaos.EffectDefinitions.World.HoldoutZone
{
    [ChaosTimedEffect("decrease_holdout_zone_charge_rate", TimedEffectType.UntilStageEnd, ConfigName = "Decrease Teleporter Charge Rate")]
    public sealed class DecreaseHoldoutZoneChargeRate : GenericHoldoutZoneModifierEffect
    {
        [EffectConfig]
        static readonly ConfigHolder<float> _chargeRateDecrease =
            ConfigFactory<float>.CreateConfig("Rate Decrease", 0.5f)
                                .Description("Percentage decrease of teleporter charge rate")
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
                _chargeRateDecrease.Value
            };
        }

        protected override void modifyChargeRate(ref float rate)
        {
            base.modifyChargeRate(ref rate);
            rate *= 1f - _chargeRateDecrease.Value;
        }
    }
}
