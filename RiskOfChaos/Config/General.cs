using BepInEx.Configuration;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RiskOfOptions;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskOfChaos.Config
{
    public static partial class Configs
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

#if DEBUG
            static ConfigEntry<bool> _debugDisable;
            const bool DEBUG_DISABLE_DEFAULT_VALUE = false;
            public static bool DebugDisable => _debugDisable?.Value ?? DEBUG_DISABLE_DEFAULT_VALUE;

            public static event Action OnDebugDisabledChanged;
#endif

            internal static void Init(ConfigFile file)
            {
                const string SECTION_NAME = "General";

                const string GUID = $"RoC_Config_{SECTION_NAME}";
                const string NAME = $"Risk of Chaos: {SECTION_NAME}";

                _timeBetweenEffects = file.Bind(new ConfigDefinition(SECTION_NAME, "Effect Timer"), TIME_BETWEEN_EFFECTS_DEFAULT_VALUE, new ConfigDescription($"How often new effects should happen"));
                ModSettingsManager.AddOption(new StepSliderOption(_timeBetweenEffects, new StepSliderConfig
                {
                    formatString = "{0:F0}s",
                    increment = 5f,
                    min = TIME_BETWEEN_EFFECTS_MIN_VALUE,
                    max = 60f * 5f
                }), GUID, NAME);

                _timeBetweenEffects.SettingChanged += static (sender, evArgs) =>
                {
                    OnTimeBetweenEffectsChanged?.Invoke();
                };

#if DEBUG
                _debugDisable = file.Bind(new ConfigDefinition(SECTION_NAME, "Debug Disable"), DEBUG_DISABLE_DEFAULT_VALUE);
                ModSettingsManager.AddOption(new CheckBoxOption(_debugDisable));

                _debugDisable.SettingChanged += static (s, e) =>
                {
                    OnDebugDisabledChanged?.Invoke();
                };
#endif

                // ModSettingsManager.SetModIcon(general_icon, GUID, NAME);
                ModSettingsManager.SetModDescription("General config options for Risk of Chaos", GUID, NAME);
            }
        }
    }
}
