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
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosTimedEffect("increase_proc_coefficients", TimedEffectType.UntilStageEnd, ConfigName = "Increase Proc Coefficients")]
    public sealed class IncreaseProcCoefficients : TimedEffect, IDamageInfoModificationProvider
    {
        [EffectConfig]
        static readonly ConfigHolder<float> _multiplierPerActivation =
            ConfigFactory<float>.CreateConfig("Proc Multiplier", 2f)
                                .OptionConfig(new StepSliderConfig
                                {
                                    formatString = "{0:F1}",
                                    min = 1.5f,
                                    max = 10f,
                                    increment = 0.5f
                                })
                                .ValueConstrictor(CommonValueConstrictors.GreaterThanOrEqualTo(1.5f))
                                .OnValueChanged(() =>
                                {
                                    if (!NetworkServer.active || !TimedChaosEffectHandler.Instance)
                                        return;

                                    TimedChaosEffectHandler.Instance.InvokeEventOnAllInstancesOfEffect<IncreaseProcCoefficients>(e => e.OnValueDirty);
                                })
                                .Build();

        [EffectNameFormatArgs]
        static string[] GetDisplayNameFormatArgs()
        {
            return new string[] { _multiplierPerActivation.Value.ToString() };
        }

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
            value.procCoefficient *= _multiplierPerActivation.Value;
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
