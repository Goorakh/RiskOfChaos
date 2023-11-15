using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;

namespace RiskOfChaos.EffectDefinitions
{
    [ChaosEffect("nothing", DefaultSelectionWeight = 0.5f, EffectWeightReductionPercentagePerActivation = 0f)]
    public sealed class Nothing : BaseEffect
    {
        [InitEffectInfo]
        public static readonly new ChaosEffectInfo EffectInfo;

        public override void OnStart()
        {
        }
    }
}
