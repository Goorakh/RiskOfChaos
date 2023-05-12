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

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosEffect("increase_fall_damage", ConfigName = "Increase Fall Damage")]
    [ChaosTimedEffect(TimedEffectType.UntilStageEnd)]
    public sealed class IncreaseFallDamage : TimedEffect, IDamageInfoModificationProvider
    {
        [InitEffectInfo]
        static readonly ChaosEffectInfo _effectInfo;

        static ConfigEntry<float> _increaseAmountConfig;
        const float INCREASE_AMOUNT_DEFAULT_VALUE = 1f;

        static float damageIncreaseAmount
        {
            get
            {
                if (_increaseAmountConfig == null)
                {
                    return INCREASE_AMOUNT_DEFAULT_VALUE;
                }
                else
                {
                    return Mathf.Max(0f, _increaseAmountConfig.Value);
                }
            }
        }

        [SystemInitializer(typeof(ChaosEffectCatalog))]
        static void InitConfigs()
        {
            _increaseAmountConfig = _effectInfo.BindConfig("Increase Amount", INCREASE_AMOUNT_DEFAULT_VALUE, new ConfigDescription("The amount to increase fall damage by"));

            addConfigOption(new StepSliderOption(_increaseAmountConfig, new StepSliderConfig
            {
                formatString = "+{0:P0}",
                min = 0f,
                max = 2f,
                increment = 0.05f
            }));
        }

        [EffectNameFormatArgs]
        static object[] GetEffectNameFormatArgs()
        {
            return new object[]
            {
                damageIncreaseAmount
            };
        }

        static float damageMultiplier => 1f + damageIncreaseAmount;

        [EffectCanActivate]
        static bool CanActivate()
        {
            return DamageInfoModificationManager.Instance && (!TimedChaosEffectHandler.Instance || !TimedChaosEffectHandler.Instance.IsTimedEffectActive(DisableFallDamage.EffectInfo));
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
