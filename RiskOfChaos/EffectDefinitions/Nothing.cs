using RiskOfChaos.EffectHandling;
using System;
using System.Collections.Generic;
using System.Text;

namespace RiskOfChaos.EffectDefinitions
{
    [ChaosEffect("Nothing", DefaultSelectionWeight = 0.5f, EffectWeightReductionPercentagePerActivation = 0f)]
    public class Nothing : BaseEffect
    {
        public override void OnStart()
        {
        }
    }
}
