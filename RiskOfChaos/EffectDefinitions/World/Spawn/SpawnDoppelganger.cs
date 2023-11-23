using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Utilities.Extensions;
using RoR2.Artifacts;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosEffect("spawn_doppelganger", DefaultSelectionWeight = 0.8f, EffectWeightReductionPercentagePerActivation = 30f)]
    public sealed class SpawnDoppelganger : BaseEffect
    {
        public override void OnStart()
        {
            DoppelgangerInvasionManager.PerformInvasion(RNG.Branch());
        }
    }
}
