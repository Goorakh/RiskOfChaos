using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Patches;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosEffect("everything_slippery", EffectStageActivationCountHardCap = 1, IsNetworked = true)]
    [ChaosTimedEffect(TimedEffectType.UntilStageEnd)]
    public sealed class EverythingSlippery : TimedEffect
    {
        public override void OnStart()
        {
            OverrideAllSurfacesSlippery.IsActive = true;
        }

        public override void OnEnd()
        {
            OverrideAllSurfacesSlippery.IsActive = false;
        }
    }
}
