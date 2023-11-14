using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfOptions.OptionConfigs;
using System;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Gravity
{
    [ChaosTimedEffect("increase_gravity", TimedEffectType.UntilStageEnd, ConfigName = "Increase Gravity", EffectWeightReductionPercentagePerActivation = 25f)]
    public sealed class IncreaseGravity : GenericMultiplyGravityEffect
    {
        [EffectConfig]
        static readonly ConfigHolder<float> _gravityIncrease =
            ConfigFactory<float>.CreateConfig("Increase per Activation", 0.5f)
                                .Description("How much gravity should increase per effect activation, 50% means the gravity is multiplied by 1.5, 100% means the gravity is multiplied by 2, etc.")
                                .OptionConfig(new StepSliderConfig
                                {
                                    min = 0f,
                                    max = 1f,
                                    increment = 0.01f,
                                    formatString = "+{0:P0}"
                                })
                                .ValueConstrictor(CommonValueConstrictors.GreaterThanOrEqualTo(0f))
                                .OnValueChanged(() =>
                                {
                                    if (!NetworkServer.active || !TimedChaosEffectHandler.Instance)
                                        return;

                                    TimedChaosEffectHandler.Instance.InvokeEventOnAllInstancesOfEffect<IncreaseGravity>(e => e.OnValueDirty);
                                })
                                .Build();

        public override event Action OnValueDirty;

        protected override float multiplier => 1f + _gravityIncrease.Value;

        [EffectNameFormatArgs]
        static object[] GetEffectNameFormatArgs()
        {
            return new object[] { _gravityIncrease.Value };
        }
    }
}
