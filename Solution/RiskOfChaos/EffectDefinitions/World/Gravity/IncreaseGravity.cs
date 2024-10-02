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

namespace RiskOfChaos.EffectDefinitions.World.Gravity
{
    [ChaosTimedEffect("increase_gravity", TimedEffectType.UntilStageEnd, ConfigName = "Increase Gravity")]
    public sealed class IncreaseGravity : GenericMultiplyGravityEffect
    {
        [EffectConfig]
        static readonly ConfigHolder<float> _gravityIncrease =
            ConfigFactory<float>.CreateConfig("Increase per Activation", 0.5f)
                                .Description("How much gravity should increase per effect activation, 50% means the gravity is multiplied by 1.5, 100% means the gravity is multiplied by 2, etc.")
                                .AcceptableValues(new AcceptableValueMin<float>(0f))
                                .OptionConfig(new StepSliderConfig
                                {
                                    min = 0f,
                                    max = 1f,
                                    increment = 0.01f,
                                    FormatString = "+{0:P0}"
                                })
                                .OnValueChanged(() =>
                                {
                                    if (!NetworkServer.active || !TimedChaosEffectHandler.Instance)
                                        return;

                                    TimedChaosEffectHandler.Instance.InvokeEventOnAllInstancesOfEffect<IncreaseGravity>(e => e.OnValueDirty);
                                })
                                .FormatsEffectName()
                                .Build();

        public override event Action OnValueDirty;

        protected override float multiplier => 1f + _gravityIncrease.Value;

        [GetEffectNameFormatter]
        static EffectNameFormatter GetNameFormatter()
        {
            return new EffectNameFormatter_GenericFloat(_gravityIncrease.Value) { ValueFormat = "P0" };
        }
    }
}
