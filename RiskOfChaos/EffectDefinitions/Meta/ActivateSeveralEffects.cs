using RiskOfChaos.EffectHandling;
using System;
using System.Collections.Generic;
using System.Text;

namespace RiskOfChaos.EffectDefinitions.Meta
{
    [ChaosEffect("ACTIVATE_SEVERAL_EFFECTS", DefaultSelectionWeight = 0.5f, EffectWeightReductionPercentagePerActivation = 15f)]
    public class ActivateSeveralEffects : BaseEffect
    {
        public override void OnStart()
        {
            for (int i = 0; i < 2; i++)
            {
                ChaosEffectDispatcher.Instance.DispatchRandomEffect(false);
            }
        }
    }
}
