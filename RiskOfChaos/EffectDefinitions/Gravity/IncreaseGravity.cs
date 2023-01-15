using BepInEx.Configuration;
using RiskOfChaos.EffectHandling;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Gravity
{
    [ChaosEffect(EFFECT_ID, ConfigName = "Increase Gravity", EffectWeightReductionPercentagePerActivation = 25f)]
    public class IncreaseGravity : GenericMultiplyGravityEffect
    {
        const string EFFECT_ID = "IncreaseGravity";

        const float GRAVITY_INCREASE_DEFAULT_VALUE = 0.5f;

        static ConfigEntry<float> _gravityIncrease;
        static float gravityIncrease => Mathf.Max(_gravityIncrease?.Value ?? GRAVITY_INCREASE_DEFAULT_VALUE, 0f);

        [SystemInitializer(typeof(ChaosEffectCatalog))]
        static void InitConfigs()
        {
            string configSectionName = getConfigSectionName(EFFECT_ID);
            if (string.IsNullOrEmpty(configSectionName))
            {
                Log.Error(ERROR_INVALID_CONFIG_SECTION_NAME);
                return;
            }

            _gravityIncrease = Main.Instance.Config.Bind(new ConfigDefinition(configSectionName, "Increase per Activation"), GRAVITY_INCREASE_DEFAULT_VALUE, new ConfigDescription("How much gravity should increase per effect activation, 50% means the gravity is multiplied by 1.5, 100% means the gravity is multiplied by 2, etc."));
            addConfigOption(new StepSliderOption(_gravityIncrease, new StepSliderConfig
            {
                min = 0f,
                max = 1f,
                increment = 0.01f,
                formatString = "+{0:P0}"
            }));
        }

        protected override float multiplier => 1f + gravityIncrease;

        [EffectNameFormatArgs]
        static object[] GetEffectNameFormatArgs()
        {
            return new object[] { gravityIncrease };
        }
    }
}
