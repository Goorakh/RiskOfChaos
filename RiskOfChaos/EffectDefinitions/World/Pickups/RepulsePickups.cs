using RiskOfChaos.Components;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Utilities;
using RoR2;

namespace RiskOfChaos.EffectDefinitions.World.Pickups
{
    [ChaosEffect("repulse_pickups")]
    [ChaosTimedEffect(90f, AllowDuplicates = false)]
    [IncompatibleEffects(typeof(AttractPickups))]
    public sealed class RepulsePickups : GenericAttractPickupsEffect
    {
        public override void OnStart()
        {
            base.OnStart();

            ItemTierPickupRulesOverride.OverrideRules = ItemTierDef.PickupRules.ConfirmAll;
        }

        public override void OnEnd()
        {
            base.OnEnd();

            ItemTierPickupRulesOverride.OverrideRules = null;
        }

        protected override void onAttractorComponentAdded(AttractToPlayers attractToPlayers)
        {
            attractToPlayers.MaxSpeed = -0.5f;
            attractToPlayers.Acceleration = 1f;
            attractToPlayers.MaxDistance = 10f;
            attractToPlayers.MinVelocityTreshold = 0f;
        }
    }
}
