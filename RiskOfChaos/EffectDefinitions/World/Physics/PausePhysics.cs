using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.ModifierController.PhysicsModification;
using System;

namespace RiskOfChaos.EffectDefinitions.World.Physics
{
    [ChaosEffect("pause_physics")]
    [ChaosTimedEffect(TimedEffectType.UntilNextEffect, AllowDuplicates = false)]
    public sealed class PausePhysics : SimplePhysicsSpeedMultiplierEffect
    {
        protected override float multiplier => 0f;
    }
}
