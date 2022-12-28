using BepInEx.Configuration;
using RiskOfChaos.EffectHandling;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Gravity
{
    [ChaosEffect(EFFECT_ID, ConfigName = "Decrease Gravity", EffectRepetitionWeightExponent = 25f)]
    public class DecreaseGravity : GenericMultiplyGravityEffect
    {
        const string EFFECT_ID = "DecreaseGravity";

        static string _configSectionName;

        const float GRAVITY_DECREASE_DEFAULT_VALUE = 0.5f;

        static ConfigEntry<float> _gravityDecrease;
        static float gravityDecrease => Mathf.Clamp01(_gravityDecrease?.Value ?? GRAVITY_DECREASE_DEFAULT_VALUE);

        [SystemInitializer(typeof(ChaosEffectCatalog))]
        static void Init()
        {
            _configSectionName = ChaosEffectCatalog.GetConfigSectionName(EFFECT_ID);

            _gravityDecrease = Main.Instance.Config.Bind(new ConfigDefinition(_configSectionName, "Decrease per Activation"), GRAVITY_DECREASE_DEFAULT_VALUE, new ConfigDescription("How much gravity should decrease per effect activation, 50% means the gravity is multiplied by 0.5, 100% means the gravity is reduced to 0, 0% means gravity doesn't change at all. etc."));
            ChaosEffectCatalog.AddEffectConfigOption(new StepSliderOption(_gravityDecrease, new StepSliderConfig
            {
                min = 0f,
                max = 1f,
                increment = 0.01f,
                formatString = "-{0:P0}"
            }));
        }

        protected override float multiplier => 1f - gravityDecrease;

        [EffectNameFormatArgs]
        static object[] GetEffectNameFormatArgs()
        {
            return new object[] { gravityDecrease };
        }
    }
}
