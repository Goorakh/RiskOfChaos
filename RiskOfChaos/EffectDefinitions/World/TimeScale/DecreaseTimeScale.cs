using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfOptions.OptionConfigs;
using System;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.TimeScale
{
    [ChaosEffect("decrease_time_scale", ConfigName = "Decrease World Speed", EffectWeightReductionPercentagePerActivation = 20f)]
    [ChaosTimedEffect(TimedEffectType.UntilStageEnd)]
    public sealed class DecreaseTimeScale : GenericMultiplyTimeScaleEffect
    {
        [EffectConfig]
        static readonly ConfigHolder<float> _timeScaleDecrease =
            ConfigFactory<float>.CreateConfig("World Speed Decrease", 0.25f)
                                .OptionConfig(new StepSliderConfig
                                {
                                    formatString = "-{0:P0}",
                                    min = 0f,
                                    max = 1f,
                                    increment = 0.01f
                                })
                                .ValueConstrictor(CommonValueConstrictors.Clamped01Float)
                                .OnValueChanged(() =>
                                {
                                    if (!NetworkServer.active || !TimedChaosEffectHandler.Instance)
                                        return;

                                    foreach (DecreaseTimeScale effectInstance in TimedChaosEffectHandler.Instance.GetActiveEffectInstancesOfType<DecreaseTimeScale>())
                                    {
                                        effectInstance.OnValueDirty?.Invoke();
                                    }
                                })
                                .Build();

        public override event Action OnValueDirty;

        protected override float multiplier => 1f - _timeScaleDecrease.Value;

        [EffectNameFormatArgs]
        static object[] GetDisplayNameFormatArgs()
        {
            return new object[] { _timeScaleDecrease.Value };
        }
    }
}
