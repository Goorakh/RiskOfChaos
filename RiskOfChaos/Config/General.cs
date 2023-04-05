using BepInEx.Configuration;
using RiskOfOptions;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using System;
using UnityEngine;

namespace RiskOfChaos
{
    partial class Configs
    {
        public static class General
        {
            static ConfigEntry<float> _timeBetweenEffects;
            const float TIME_BETWEEN_EFFECTS_MIN_VALUE = 5f;
            const float TIME_BETWEEN_EFFECTS_DEFAULT_VALUE = 60f;
            public static float TimeBetweenEffects
            {
                get
                {
                    if (_timeBetweenEffects != null)
                    {
                        return Mathf.Max(_timeBetweenEffects.Value, TIME_BETWEEN_EFFECTS_MIN_VALUE);
                    }
                    else
                    {
                        return TIME_BETWEEN_EFFECTS_DEFAULT_VALUE;
                    }
                }
            }

            public static event Action OnTimeBetweenEffectsChanged;

            internal static void Init(ConfigFile file)
            {
                const string GENERAL_SECTION_NAME = "General";

                _timeBetweenEffects = file.Bind(new ConfigDefinition(GENERAL_SECTION_NAME, "Effect Timer"), TIME_BETWEEN_EFFECTS_DEFAULT_VALUE, new ConfigDescription($"How often new effects should happen"));
                ModSettingsManager.AddOption(new StepSliderOption(_timeBetweenEffects, new StepSliderConfig
                {
                    formatString = "{0:F0}s",
                    increment = 5f,
                    min = TIME_BETWEEN_EFFECTS_MIN_VALUE,
                    max = 60f * 5f
                }), CONFIG_GUID, CONFIG_NAME);

                _timeBetweenEffects.SettingChanged += static (sender, evArgs) =>
                {
                    OnTimeBetweenEffectsChanged?.Invoke();
                };
            }
        }
    }
}
