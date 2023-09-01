using BepInEx.Configuration;
using RiskOfChaos.ConfigHandling;
using RiskOfOptions.OptionConfigs;
using UnityEngine;

namespace RiskOfChaos
{
    partial class Configs
    {
        public static class General
        {
            public static readonly ConfigHolder<bool> DisableEffectDispatching =
                ConfigFactory<bool>.CreateConfig("Disable Effect Activation", false)
                                   .Description("If effect activation should be disabled completely")
                                   .OptionConfig(new CheckBoxConfig())
                                   .Build();

            const float TIME_BETWEEN_EFFECTS_MIN_VALUE = 5f;
            public static readonly ConfigHolder<float> TimeBetweenEffects =
                ConfigFactory<float>.CreateConfig("Effect Timer", 60f)
                                    .Description("How often new effects should happen")
                                    .OptionConfig(new StepSliderConfig
                                    {
                                        checkIfDisabled = () => DisableEffectDispatching.Value,
                                        formatString = "{0:F0}s",
                                        increment = 5f,
                                        min = TIME_BETWEEN_EFFECTS_MIN_VALUE,
                                        max = 60f * 5f
                                    })
                                    .ValueConstrictor(CommonValueConstrictors.GreaterThanOrEqualTo(TIME_BETWEEN_EFFECTS_MIN_VALUE))
                                    .Build();

            public static readonly ConfigHolder<Color> ActiveEffectsTextColor =
                ConfigFactory<Color>.CreateConfig("Active Effect Text Color", Color.white)
                                    .Description("The color of the effect names in the \"Active Effects\" list")
                                    .OptionConfig(new ColorOptionConfig())
                                    .Build();

            internal static void Bind(ConfigFile file)
            {
                const string GENERAL_SECTION_NAME = "General";

                DisableEffectDispatching.Bind(file, GENERAL_SECTION_NAME, CONFIG_GUID, CONFIG_NAME);

                TimeBetweenEffects.Bind(file, GENERAL_SECTION_NAME, CONFIG_GUID, CONFIG_NAME);

                ActiveEffectsTextColor.Bind(file, GENERAL_SECTION_NAME, CONFIG_GUID, CONFIG_NAME);
            }
        }
    }
}
