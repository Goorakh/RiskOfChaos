using RiskOfChaos.Components;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;

namespace RiskOfChaos.EffectDefinitions.World.Pickups
{
    [ChaosEffect("repulse_pickups")]
    [ChaosTimedEffect(90f, AllowDuplicates = false)]
    public sealed class RepulsePickups : GenericAttractPickupsEffect
    {
        [InitEffectInfo]
        public static readonly ChaosEffectInfo EffectInfo;

        [EffectCanActivate]
        static bool CanActivate()
        {
            return TimedChaosEffectHandler.Instance && !TimedChaosEffectHandler.Instance.IsTimedEffectActive(AttractPickups.EffectInfo);
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
