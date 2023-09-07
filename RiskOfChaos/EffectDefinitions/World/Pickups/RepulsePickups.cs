using RiskOfChaos.Components;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.Utilities;
using RiskOfOptions.OptionConfigs;
using RoR2;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Pickups
{
    [ChaosTimedEffect("repulse_pickups", 90f, AllowDuplicates = false)]
    public sealed class RepulsePickups : GenericAttractPickupsEffect
    {
        [EffectConfig]
        static readonly ConfigHolder<float> _speedMultiplier =
            ConfigFactory<float>.CreateConfig("Repulse Strength Multiplier", 1f)
                                .Description("Multiplies the strength of the effect")
                                .OptionConfig(new StepSliderConfig
                                {
                                    formatString = "{0:F1}x",
                                    min = 0f,
                                    max = 5f,
                                    increment = 0.1f
                                })
                                .ValueConstrictor(CommonValueConstrictors.GreaterThanOrEqualTo(0f))
                                .OnValueChanged(() =>
                                {
                                    if (!NetworkServer.active || !TimedChaosEffectHandler.Instance)
                                        return;

                                    foreach (RepulsePickups effectInstance in TimedChaosEffectHandler.Instance.GetActiveEffectInstancesOfType<RepulsePickups>())
                                    {
                                        effectInstance?.updateAllAttractorComponents();
                                    }
                                })
                                .Build();

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
            base.onAttractorComponentAdded(attractToPlayers);

            attractToPlayers.MaxDistance = 10f;
            attractToPlayers.MinVelocityTreshold = 0f;
        }

        protected override void updateAttractorComponent(AttractToPlayers attractToPlayers)
        {
            base.updateAttractorComponent(attractToPlayers);

            attractToPlayers.MaxSpeed = -0.5f * _speedMultiplier.Value;
            attractToPlayers.Acceleration = 1f * (((_speedMultiplier.Value - 1) / 2f) + 1);
        }
    }
}
