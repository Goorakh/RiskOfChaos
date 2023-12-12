using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.Formatting;
using RiskOfOptions.OptionConfigs;

namespace RiskOfChaos.EffectDefinitions.World.PurchaseInteractionCost
{
    [ChaosTimedEffect("decrease_interactable_costs", TimedEffectType.UntilStageEnd, DefaultSelectionWeight = 0.8f, ConfigName = "Decrease Chest Prices")]
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

        [GetEffectNameFormatter]
        static EffectNameFormatter GetNameFormatter()
        {
            return new EffectNameFormatter_GenericFloat(_decreaseAmount.Value) { ValueFormat = "P0" };
        }

        protected override float multiplier => 1f - _decreaseAmount.Value;
    }
}
