using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;

namespace RiskOfChaos.EffectDefinitions
{
    [ChaosEffect("Nothing", DefaultSelectionWeight = 0.5f, EffectWeightReductionPercentagePerActivation = 0f)]
    public class Nothing : BaseEffect
    {
        [InitEffectInfo]
        public static readonly ChaosEffectInfo EffectInfo;

        public override void OnStart()
        {
        }
    }
}
