using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfOptions.OptionConfigs;

namespace RiskOfChaos.EffectDefinitions.World.PurchaseInteractionCost
{
    [ChaosEffect("decrease_interactable_costs", DefaultSelectionWeight = 0.8f, ConfigName = "Decrease Chest Prices")]
    [ChaosTimedEffect(TimedEffectType.UntilStageEnd)]
    public sealed class DecreaseInteractableCosts : GenericMultiplyPurchaseInteractionCostsEffect
    {
        [EffectConfig]
        static readonly ConfigHolder<float> _decreaseAmount =
            ConfigFactory<float>.CreateConfig("Decrease Amount", 0.25f)
                                .Description("The amount to decrease costs by")
                                .OptionConfig(new StepSliderConfig
                                {
                                    formatString = "-{0:P0}",
                                    min = 0f,
                                    max = 1f,
                                    increment = 0.05f
                                })
                                .ValueConstrictor(CommonValueConstrictors.Clamped01Float)
                                .Build();

        [EffectNameFormatArgs]
        static object[] GetDisplayNameFormatArgs()
        {
            return new object[] { _decreaseAmount.Value };
        }

        protected override float multiplier => 1f - _decreaseAmount.Value;
    }
}
