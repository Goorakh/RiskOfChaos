using BepInEx.Configuration;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.ModifierController.DamageInfo;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2;
using System;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosEffect("increase_proc_coefficients", ConfigName = "Increase Proc Coefficients")]
    [ChaosTimedEffect(TimedEffectType.UntilStageEnd)]
    public sealed class IncreaseProcCoefficients : TimedEffect, IDamageInfoModificationProvider
    {
        [InitEffectInfo]
        static readonly ChaosEffectInfo _effectInfo;

        static ConfigEntry<float> _multiplierPerActivationConfig;
        const float MULTIPLIER_PER_ACTIVATION_DEFAULT_VALUE = 2f;

        static float multiplierPerActivation
        {
            get
            {
                if (_multiplierPerActivationConfig == null)
                {
                    return MULTIPLIER_PER_ACTIVATION_DEFAULT_VALUE;
                }
                else
                {
                    return Mathf.Max(_multiplierPerActivationConfig.Value, 1f);
                }
            }
        }

        [SystemInitializer(typeof(ChaosEffectCatalog))]
        static void InitConfigs()
        {
            _multiplierPerActivationConfig = _effectInfo.BindConfig("Proc Multiplier", MULTIPLIER_PER_ACTIVATION_DEFAULT_VALUE, null);

            _multiplierPerActivationConfig.SettingChanged += (o, e) =>
            {
                if (!NetworkServer.active || !TimedChaosEffectHandler.Instance)
                    return;

                foreach (IncreaseProcCoefficients effectInstance in TimedChaosEffectHandler.Instance.GetActiveEffectInstancesOfType<IncreaseProcCoefficients>())
                {
                    effectInstance.OnValueDirty?.Invoke();
                }
            };

            addConfigOption(new StepSliderOption(_multiplierPerActivationConfig, new StepSliderConfig
            {
                formatString = "{0:F1}",
                min = 1f,
                max = 10f,
                increment = 0.5f
            }));
        }

        [EffectNameFormatArgs]
        static object[] GetDisplayNameFormatArgs()
        {
            return new object[]
            {
                multiplierPerActivation
            };
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
            value.procCoefficient *= multiplierPerActivation;
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
