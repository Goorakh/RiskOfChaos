using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfOptions.OptionConfigs;
using RoR2;

namespace RiskOfChaos.EffectDefinitions.World.HoldoutZone
{
    [ChaosTimedEffect("increase_holdout_zone_charge_rate", TimedEffectType.UntilStageEnd, ConfigName = "Increase Teleporter Charge Rate")]
    public sealed class IncreaseHoldoutZoneChargeRate : GenericHoldoutZoneModifierEffect
    {
        [EffectConfig]
        static readonly ConfigHolder<float> _chargeRateIncrease =
            ConfigFactory<float>.CreateConfig("Rate Increase", 0.5f)
                                .Description("Percentage increase of teleporter charge rate")
                                .OptionConfig(new StepSliderConfig
                                {
                                    formatString = "+{0:P0}",
                                    increment = 0.01f,
                                    min = 0f,
                                    max = 2f
                                })
                                .ValueConstrictor(CommonValueConstrictors.GreaterThanOrEqualTo(0f))
                                .Build();

        [EffectNameFormatArgs]
        static string[] GetEffectNameFormatArgs()
        {
            return new string[] { _chargeRateIncrease.Value.ToString("P0") };
        }

        protected override void modifyChargeRate(HoldoutZoneController controller, ref float rate)
        {
            base.modifyChargeRate(controller, ref rate);
            rate *= 1f + _chargeRateIncrease.Value;
        }
    }
}
