using BepInEx.Configuration;
using RiskOfChaos.ConfigHandling;
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
    [ChaosTimedEffect("decrease_time_scale", TimedEffectType.UntilStageEnd, ConfigName = "Decrease World Speed")]
    public sealed class DecreaseTimeScale : GenericMultiplyTimeScaleEffect
    {
        [EffectConfig]
        static readonly ConfigHolder<float> _timeScaleDecrease =
            ConfigFactory<float>.CreateConfig("World Speed Decrease", 0.25f)
                                .AcceptableValues(new AcceptableValueRange<float>(0f, 1f))
                                .OptionConfig(new StepSliderConfig
                                {
                                    formatString = "-{0:P0}",
                                    min = 0f,
                                    max = 1f,
                                    increment = 0.01f
                                })
                                .OnValueChanged(() =>
                                {
                                    if (!NetworkServer.active || !TimedChaosEffectHandler.Instance)
                                        return;

                                    TimedChaosEffectHandler.Instance.InvokeEventOnAllInstancesOfEffect<DecreaseTimeScale>(e => e.OnValueDirty);
                                })
                                .FormatsEffectName()
                                .Build();

        public override event Action OnValueDirty;

        protected override float multiplier => 1f - _timeScaleDecrease.Value;

        [GetEffectNameFormatter]
        static EffectNameFormatter GetNameFormatter()
        {
            return new EffectNameFormatter_GenericFloat(_timeScaleDecrease.Value) { ValueFormat = "P0" };
        }
    }
}
