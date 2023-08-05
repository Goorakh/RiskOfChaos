using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.ModifierController.Damage;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosEffect("increase_fall_damage", ConfigName = "Increase Fall Damage")]
    [ChaosTimedEffect(TimedEffectType.UntilStageEnd)]
    [IncompatibleEffects(typeof(DisableFallDamage))]
    public sealed class IncreaseFallDamage : TimedEffect, IDamageInfoModificationProvider
    {
        [EffectConfig]
        static readonly ConfigHolder<float> _damageIncreaseAmount =
            ConfigFactory<float>.CreateConfig("Increase Amount", 1f)
                                .Description("The amount to increase fall damage by")
                                .OptionConfig(new StepSliderConfig
                                {
                                    formatString = "+{0:P0}",
                                    min = 0f,
                                    max = 2f,
                                    increment = 0.05f
                                })
                                .ValueConstrictor(ValueConstrictors.GreaterThanOrEqualTo(0f))
                                .OnValueChanged(() =>
                                {
                                    if (!NetworkServer.active || !TimedChaosEffectHandler.Instance)
                                        return;

                                    foreach (IncreaseFallDamage effectInstance in TimedChaosEffectHandler.Instance.GetActiveEffectInstancesOfType<IncreaseFallDamage>())
                                    {
                                        effectInstance.OnValueDirty?.Invoke();
                                    }
                                })
                                .Build();

        [EffectNameFormatArgs]
        static object[] GetEffectNameFormatArgs()
        {
            return new object[]
            {
                _damageIncreaseAmount.Value
            };
        }

        static float damageMultiplier => 1f + _damageIncreaseAmount.Value;

        [EffectCanActivate]
        static bool CanActivate()
        {
            return DamageInfoModificationManager.Instance;
        }

        public override void OnStart()
        {
            DamageInfoModificationManager.Instance.RegisterModificationProvider(this);
        }

        public event Action OnValueDirty;

        public void ModifyValue(ref DamageInfo value)
        {
            if ((value.damageType & DamageType.FallDamage) != 0)
            {
                value.damage *= damageMultiplier;

                if (damageMultiplier > 1f)
                {
                    value.damageType &= ~DamageType.NonLethal;
                    value.damageType |= DamageType.BypassOneShotProtection;
                }
            }
        }

        public override void OnEnd()
        {
            if (DamageInfoModificationManager.Instance)
            {
                DamageInfoModificationManager.Instance.UnregisterModificationProvider(this);
            }
        }
    }
}
