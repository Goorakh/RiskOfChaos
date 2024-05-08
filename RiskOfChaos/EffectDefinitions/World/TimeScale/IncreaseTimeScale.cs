using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.Formatting;
using RiskOfOptions.OptionConfigs;
using System;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.TimeScale
{
    [ChaosTimedEffect("increase_time_scale", TimedEffectType.UntilStageEnd, ConfigName = "Increase World Speed")]
    public sealed class IncreaseTimeScale : GenericMultiplyTimeScaleEffect
    {
        [EffectConfig]
        static readonly ConfigHolder<float> _timeScaleIncrease =
            ConfigFactory<float>.CreateConfig("World Speed Increase", 0.25f)
                                .AcceptableValues(new AcceptableValueMin<float>(0f))
                                .OptionConfig(new StepSliderConfig
                                {
                                    formatString = "+{0:P0}",
                                    min = 0f,
                                    max = 1f,
                                    increment = 0.01f
                                })
                                .OnValueChanged(() =>
                                {
                                    if (!NetworkServer.active || !TimedChaosEffectHandler.Instance)
                                        return;

                                    TimedChaosEffectHandler.Instance.InvokeEventOnAllInstancesOfEffect<IncreaseTimeScale>(e => e.OnValueDirty);
                                })
                                .FormatsEffectName()
                                .Build();

        public override event Action OnValueDirty;

        protected override float multiplier => 1f + _timeScaleIncrease.Value;

        [GetEffectNameFormatter]
        static EffectNameFormatter GetNameFormatter()
        {
            return new EffectNameFormatter_GenericFloat(_timeScaleIncrease.Value) { ValueFormat = "P0" };
        }
    }
}
