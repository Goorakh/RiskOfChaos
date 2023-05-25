using RiskOfChaos.EffectHandling.EffectClassAttributes;

namespace RiskOfChaos.EffectDefinitions.World.Physics
{
    [ChaosEffect("pause_physics")]
    [ChaosTimedEffect(90f, AllowDuplicates = false)]
    public sealed class PausePhysics : SimplePhysicsSpeedMultiplierEffect
    {
        protected override float multiplier => 0f;
    }
}
