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
    [ChaosTimedEffect("repulse_pickups", 90f, AllowDuplicates = false)]
    public sealed class RepulsePickups : GenericAttractPickupsEffect
    {
        [EffectConfig]
        static readonly ConfigHolder<float> _speedMultiplier =
            ConfigFactory<float>.CreateConfig("Repulse Strength Multiplier", 1f)
                                .Description("Multiplies the strength of the effect")
                                .AcceptableValues(new AcceptableValueMin<float>(0f))
                                .OptionConfig(new FloatFieldConfig { Min = 0f, FormatString = "{0}x" })
                                .OnValueChanged(() =>
                                {
                                    if (!NetworkServer.active || !ChaosEffectTracker.Instance)
                                        return;

                                    foreach (RepulsePickups effectInstance in ChaosEffectTracker.Instance.OLD_GetActiveEffectInstancesOfType<RepulsePickups>())
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
