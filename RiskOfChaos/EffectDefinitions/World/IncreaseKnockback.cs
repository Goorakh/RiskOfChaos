using BepInEx.Configuration;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.ModifierController.Knockback;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2;
using System;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosEffect("increase_knockback", ConfigName = "Increase Knockback", EffectWeightReductionPercentagePerActivation = 30f)]
    public sealed class IncreaseKnockback : TimedEffect, IKnockbackModificationProvider
    {
        [InitEffectInfo]
        static readonly ChaosEffectInfo _effectInfo;

        static ConfigEntry<float> _knockbackMultiplierConfig;
        const float KNOCKBACK_MULTIPLIER_DEFAULT_VALUE = 3f;

        const float KNOCKBACK_MULTIPLIER_INCREMENT = 0.1f;
        const float KNOCKBACK_MULTIPLIER_MIN_VALUE = 1f + KNOCKBACK_MULTIPLIER_INCREMENT;

        static float knockbackMultiplier
        {
            get
            {
                if (_knockbackMultiplierConfig == null)
                {
                    return KNOCKBACK_MULTIPLIER_DEFAULT_VALUE;
                }
                else
                {
                    return Mathf.Max(_knockbackMultiplierConfig.Value, KNOCKBACK_MULTIPLIER_MIN_VALUE);
                }
            }
        }

        [SystemInitializer(typeof(ChaosEffectCatalog))]
        static void InitConfigs()
        {
            _knockbackMultiplierConfig = Main.Instance.Config.Bind(new ConfigDefinition(_effectInfo.ConfigSectionName, "Knockback Multiplier"), KNOCKBACK_MULTIPLIER_DEFAULT_VALUE, new ConfigDescription("The multiplier used to increase knockback while the effect is active"));

            addConfigOption(new StepSliderOption(_knockbackMultiplierConfig, new StepSliderConfig
            {
                formatString = "{0:F1}x",
                min = KNOCKBACK_MULTIPLIER_MIN_VALUE,
                max = 15f,
                increment = KNOCKBACK_MULTIPLIER_INCREMENT
            }));
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return KnockbackModificationManager.Instance;
        }

        [EffectNameFormatArgs]
        static object[] GetEffectNameFormatArgs()
        {
            return new object[] { knockbackMultiplier };
        }

        public event Action OnValueDirty;

        public void ModifyValue(ref float value)
        {
            value *= knockbackMultiplier;
        }

        public override TimedEffectType TimedType => TimedEffectType.UntilStageEnd;

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
