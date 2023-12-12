using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.Formatting;
using RiskOfChaos.ModifierController.Damage;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosTimedEffect("increase_fall_damage", TimedEffectType.UntilStageEnd, ConfigName = "Increase Fall Damage")]
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
                                .ValueConstrictor(CommonValueConstrictors.GreaterThanOrEqualTo(0f))
                                .OnValueChanged(() =>
                                {
                                    if (!NetworkServer.active || !TimedChaosEffectHandler.Instance)
                                        return;

                                    TimedChaosEffectHandler.Instance.InvokeEventOnAllInstancesOfEffect<IncreaseFallDamage>(e => e.OnValueDirty);
                                })
                                .Build();

        [GetEffectNameFormatter]
        static EffectNameFormatter GetEffectNameFormatter()
        {
            return new EffectNameFormatter_GenericFloat(_damageIncreaseAmount.Value) { ValueFormat = "P0" };
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
