using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Patches;
using RoR2;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosEffect("everything_slippery", EffectActivationCountHardCap = 1)]
    public sealed class EverythingSlippery : BaseEffect
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
