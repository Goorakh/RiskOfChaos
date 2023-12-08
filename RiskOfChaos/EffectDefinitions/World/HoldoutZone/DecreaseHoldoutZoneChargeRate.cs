using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfOptions.OptionConfigs;
using RoR2;

namespace RiskOfChaos.EffectDefinitions.World.HoldoutZone
{
    [ChaosTimedEffect("decrease_holdout_zone_charge_rate", TimedEffectType.UntilStageEnd, ConfigName = "Decrease Teleporter Charge Rate")]
    public sealed class DecreaseHoldoutZoneChargeRate : GenericHoldoutZoneModifierEffect
    {
        [EffectConfig]
        static readonly ConfigHolder<float> _chargeRateDecrease =
            ConfigFactory<float>.CreateConfig("Rate Decrease", 0.25f)
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
        static string[] GetEffectNameFormatArgs()
        {
            return new string[] { _chargeRateDecrease.Value.ToString("P0") };
        }

        protected override void modifyChargeRate(HoldoutZoneController controller, ref float rate)
        {
            base.modifyChargeRate(controller, ref rate);
            rate *= 1f - _chargeRateDecrease.Value;
        }
    }
}
