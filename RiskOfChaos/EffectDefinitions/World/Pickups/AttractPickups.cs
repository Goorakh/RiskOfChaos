using RiskOfChaos.Components;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.Utilities;
using RiskOfOptions.OptionConfigs;
using RoR2;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Pickups
{
    [ChaosTimedEffect("attract_pickups", 90f, AllowDuplicates = false)]
    public sealed class AttractPickups : GenericAttractPickupsEffect
    {
        [EffectConfig]
        static readonly ConfigHolder<float> _speedMultiplier =
            ConfigFactory<float>.CreateConfig("Attract Strength Multiplier", 1f)
                                .Description("Multiplies the strength of the effect")
                                .AcceptableValues(new AcceptableValueMin<float>(0f))
                                .OptionConfig(new StepSliderConfig
                                {
                                    formatString = "{0:F1}x",
                                    min = 0f,
                                    max = 5f,
                                    increment = 0.1f
                                })
                                .OnValueChanged(() =>
                                {
                                    if (!NetworkServer.active || !TimedChaosEffectHandler.Instance)
                                        return;

                                    foreach (AttractPickups effectInstance in TimedChaosEffectHandler.Instance.GetActiveEffectInstancesOfType<AttractPickups>())
                                    {
                                        effectInstance?.updateAllAttractorComponents();
                                    }
                                })
                                .Build();

        public override void OnStart()
        {
            base.OnStart();

            ItemTierPickupRulesOverride.OverrideRules = ItemTierDef.PickupRules.Default;
        }

        public override void OnEnd()
        {
            base.OnEnd();

            ItemTierPickupRulesOverride.OverrideRules = null;
        }

        protected override void onAttractorComponentAdded(AttractToPlayers attractToPlayers)
        {
            base.onAttractorComponentAdded(attractToPlayers);

            attractToPlayers.MaxDistance = 20f;
            attractToPlayers.MinVelocityTreshold = 0f;
        }

        protected override void updateAttractorComponent(AttractToPlayers attractToPlayers)
        {
            base.updateAttractorComponent(attractToPlayers);

            attractToPlayers.MaxSpeed = 0.5f * _speedMultiplier.Value;
            attractToPlayers.Acceleration = 1f * (((_speedMultiplier.Value - 1) / 2f) + 1);

            attractToPlayers.DynamicFrictionOverride = 0.1f * (((_speedMultiplier.Value - 1) / 2f) + 1);
        }
    }
}
