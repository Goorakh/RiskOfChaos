using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfOptions.OptionConfigs;

namespace RiskOfChaos.EffectDefinitions.World.PurchaseInteractionCost
{
    [ChaosEffect("increase_interactable_costs", DefaultSelectionWeight = 0.8f, ConfigName = "Increase Chest Prices")]
    [ChaosTimedEffect(TimedEffectType.UntilStageEnd)]
    public sealed class IncreaseInteractableCosts : GenericMultiplyPurchaseInteractionCostsEffect
    {
        const float INCREASE_AMOUNT_MIN_VALUE = 0.05f;

        [EffectConfig]
        static readonly ConfigHolder<float> _increaseAmount =
            ConfigFactory<float>.CreateConfig("Increase Amount", 0.25f)
                                .Description("The amount to increase costs by")
                                .OptionConfig(new StepSliderConfig
                                {
                                    formatString = "+{0:P0}",
                                    min = INCREASE_AMOUNT_MIN_VALUE,
                                    max = 2f,
                                    increment = 0.05f
                                })
                                .ValueConstrictor(ValueConstrictors.GreaterThanOrEqualTo(INCREASE_AMOUNT_MIN_VALUE))
                                .Build();

        [EffectNameFormatArgs]
        static object[] GetDisplayNameFormatArgs()
        {
            return new object[] { _increaseAmount.Value };
        }

        protected override float multiplier => 1f + _increaseAmount.Value;
    }
}
