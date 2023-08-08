using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.ModifierController.Knockback;
using RiskOfOptions.OptionConfigs;
using System;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosEffect("increase_knockback", ConfigName = "Increase Knockback", EffectWeightReductionPercentagePerActivation = 30f)]
    [ChaosTimedEffect(TimedEffectType.UntilStageEnd)]
    public sealed class IncreaseKnockback : TimedEffect, IKnockbackModificationProvider
    {
        const float KNOCKBACK_MULTIPLIER_INCREMENT = 0.1f;
        const float KNOCKBACK_MULTIPLIER_MIN_VALUE = 1f + KNOCKBACK_MULTIPLIER_INCREMENT;

        [EffectConfig]
        static readonly ConfigHolder<float> _knockbackMultiplier =
            ConfigFactory<float>.CreateConfig("Knockback Multiplier", 3f)
                                .Description("The multiplier used to increase knockback while the effect is active")
                                .OptionConfig(new StepSliderConfig
                                {
                                    formatString = "{0:F1}x",
                                    min = KNOCKBACK_MULTIPLIER_MIN_VALUE,
                                    max = 15f,
                                    increment = KNOCKBACK_MULTIPLIER_INCREMENT
                                })
                                .ValueConstrictor(CommonValueConstrictors.GreaterThanOrEqualTo(KNOCKBACK_MULTIPLIER_MIN_VALUE))
                                .OnValueChanged(() =>
                                {
                                    if (!NetworkServer.active || !TimedChaosEffectHandler.Instance)
                                        return;

                                    foreach (IncreaseKnockback effectInstance in TimedChaosEffectHandler.Instance.GetActiveEffectInstancesOfType<IncreaseKnockback>())
                                    {
                                        effectInstance.OnValueDirty?.Invoke();
                                    }
                                })
                                .Build();

        [EffectCanActivate]
        static bool CanActivate()
        {
            return KnockbackModificationManager.Instance;
        }

        [EffectNameFormatArgs]
        static object[] GetEffectNameFormatArgs()
        {
            return new object[] { _knockbackMultiplier.Value };
        }

        public event Action OnValueDirty;

        public void ModifyValue(ref float value)
        {
            value *= _knockbackMultiplier.Value;
        }

        public override void OnStart()
        {
            KnockbackModificationManager.Instance.RegisterModificationProvider(this);
        }

        public override void OnEnd()
        {
            if (KnockbackModificationManager.Instance)
            {
                KnockbackModificationManager.Instance.UnregisterModificationProvider(this);
            }
        }
    }
}
