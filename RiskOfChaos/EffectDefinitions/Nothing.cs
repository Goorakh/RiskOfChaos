using RiskOfChaos.EffectHandling.EffectClassAttributes;

namespace RiskOfChaos.EffectDefinitions
{
    [ChaosEffect(EFFECT_ID, DefaultSelectionWeight = 0.5f, EffectWeightReductionPercentagePerActivation = 0f)]
    public class Nothing : BaseEffect
    {
        public const string EFFECT_ID = "Nothing";

        public override void OnStart()
        {
        }
    }
}
