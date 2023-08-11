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
    [ChaosEffect("decrease_gravity", ConfigName = "Decrease Gravity", EffectWeightReductionPercentagePerActivation = 25f)]
    [ChaosTimedEffect(TimedEffectType.UntilStageEnd)]
    public sealed class DecreaseGravity : GenericMultiplyGravityEffect
    {
        [EffectConfig]
        static readonly ConfigHolder<float> _gravityDecrease =
            ConfigFactory<float>.CreateConfig("Decrease per Activation", 0.5f)
                                .Description("How much gravity should decrease per effect activation, 50% means the gravity is multiplied by 0.5, 100% means the gravity is reduced to 0, 0% means gravity doesn't change at all. etc.")
                                .OptionConfig(new StepSliderConfig
                                {
                                    min = 0f,
                                    max = 1f,
                                    increment = 0.01f,
                                    formatString = "-{0:P0}"
                                })
                                .ValueConstrictor(CommonValueConstrictors.Clamped01Float)
                                .OnValueChanged(() =>
                                {
                                    if (!NetworkServer.active || !TimedChaosEffectHandler.Instance)
                                        return;

                                    foreach (DecreaseGravity effectInstance in TimedChaosEffectHandler.Instance.GetActiveEffectInstancesOfType<DecreaseGravity>())
                                    {
                                        effectInstance.OnValueDirty?.Invoke();
                                    }
                                })
                                .Build();

        public override event Action OnValueDirty;

        protected override float multiplier => 1f - _gravityDecrease.Value;

        [EffectNameFormatArgs]
        static object[] GetEffectNameFormatArgs()
        {
            return new object[] { _gravityDecrease.Value };
        }
    }
}
