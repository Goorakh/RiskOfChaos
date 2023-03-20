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

            static ConfigEntry<bool> _useLocalhostConnect;
            const bool USE_LOCALHOST_CONNECT_DEFAULT_VALUE = false;
            public static bool UseLocalhostConnect => _useLocalhostConnect?.Value ?? USE_LOCALHOST_CONNECT_DEFAULT_VALUE;
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
                const string DEBUG_SECTION_NAME = "Debug";

                _debugDisable = file.Bind(new ConfigDefinition(DEBUG_SECTION_NAME, "Debug Disable"), DEBUG_DISABLE_DEFAULT_VALUE);
                ModSettingsManager.AddOption(new CheckBoxOption(_debugDisable), GUID, NAME);

                _debugDisable.SettingChanged += static (s, e) =>
                {
                    OnDebugDisabledChanged?.Invoke();
                };

                _useLocalhostConnect = file.Bind(new ConfigDefinition(DEBUG_SECTION_NAME, "Use Localhost Connect"), USE_LOCALHOST_CONNECT_DEFAULT_VALUE);
                ModSettingsManager.AddOption(new CheckBoxOption(_useLocalhostConnect), GUID, NAME);

                On.RoR2.Networking.NetworkManagerSystem.ClientSendAuth += (orig, self, conn) =>
                {
                    if (UseLocalhostConnect)
                        return;

                    orig(self, conn);
                };

                // Connecting to localhost this way makes entitlements not work, so just force them all to be enabled
                On.RoR2.PlayerCharacterMasterControllerEntitlementTracker.HasEntitlement += (orig, self, entitlementDef) =>
                {
                    return UseLocalhostConnect || orig(self, entitlementDef);
                };
#endif

                // ModSettingsManager.SetModIcon(general_icon, GUID, NAME);
                ModSettingsManager.SetModDescription("General config options for Risk of Chaos", GUID, NAME);
            }
        }
    }
}
