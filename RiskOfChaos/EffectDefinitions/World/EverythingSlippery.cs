using RiskOfChaos.EffectHandling;
using RiskOfChaos.Patches;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosEffect("everything_slippery", EffectActivationCountHardCap = 1)]
    public class EverythingSlippery : BaseEffect
    {
        public override void OnStart()
        {
            OverrideAllSurfacesSlippery.NetworkIsActive = true;

            Stage.onServerStageComplete += Stage_onServerStageComplete;
        }

        static void Stage_onServerStageComplete(Stage _)
        {
            OverrideAllSurfacesSlippery.NetworkIsActive = false;

            Stage.onServerStageComplete -= Stage_onServerStageComplete;
        }
    }
}
