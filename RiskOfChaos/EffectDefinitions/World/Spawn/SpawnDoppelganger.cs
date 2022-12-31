using RiskOfChaos.EffectHandling;
using RoR2.Artifacts;
using System;
using System.Collections.Generic;
using System.Text;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosEffect("spawn_doppelganger", DefaultSelectionWeight = 0.8f, EffectRepetitionWeightExponent = 30f)]
    public class SpawnDoppelganger : BaseEffect
    {
        public override void OnStart()
        {
            DoppelgangerInvasionManager.PerformInvasion(new Xoroshiro128Plus(RNG.nextUlong));
        }
    }
}
