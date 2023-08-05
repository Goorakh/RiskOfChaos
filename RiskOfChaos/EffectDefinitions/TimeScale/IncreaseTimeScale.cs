using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfOptions.OptionConfigs;
using System;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.TimeScale
{
    [ChaosEffect("increase_time_scale", ConfigName = "Increase World Speed", EffectWeightReductionPercentagePerActivation = 20f)]
    [ChaosTimedEffect(TimedEffectType.UntilStageEnd)]
    public sealed class IncreaseTimeScale : GenericMultiplyTimeScaleEffect
    {
        [EffectConfig]
        static readonly ConfigHolder<float> _timeScaleIncrease =
            ConfigFactory<float>.CreateConfig("World Speed Increase", 0.25f)
                                .OptionConfig(new StepSliderConfig
                                {
                                    formatString = "+{0:P0}",
                                    min = 0f,
                                    max = 1f,
                                    increment = 0.01f
                                })
                                .ValueConstrictor(ValueConstrictors.GreaterThanOrEqualTo(0f))
                                .OnValueChanged(() =>
                                {
                                    if (!NetworkServer.active || !TimedChaosEffectHandler.Instance)
                                        return;

                                    foreach (IncreaseTimeScale effectInstance in TimedChaosEffectHandler.Instance.GetActiveEffectInstancesOfType<IncreaseTimeScale>())
                                    {
                                        effectInstance.OnValueDirty?.Invoke();
                                    }
                                })
                                .Build();

        public override event Action OnValueDirty;

        protected override float multiplier => 1f + _timeScaleIncrease.Value;

        [EffectNameFormatArgs]
        static object[] GetDisplayNameFormatArgs()
        {
            return new object[] { _timeScaleIncrease.Value };
        }
    }
}
