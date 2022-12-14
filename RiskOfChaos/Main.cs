using BepInEx;
using BepInEx.Configuration;
using Facepunch.Steamworks;
using R2API;
using R2API.Utils;
using RiskOfChaos.EffectHandling;
using RiskOfOptions;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RiskOfChaos
{
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInDependency("com.rune580.riskofoptions")]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class Main : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Gorakh";
        public const string PluginName = "RiskOfChaos";
        public const string PluginVersion = "1.0.0";

        internal static Main Instance;

        public static ConfigEntry<ChaosEffectMode> EffectActivationMode { get; private set; }
        public static ConfigEntry<float> TimeBetweenEffects { get; private set; }
        public static ConfigEntry<int> EffectOverlap { get; private set; }

        void Awake()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            Log.Init(Logger);

            Instance = this;

            LanguageFileHandler.Init();

            initConfigs();

            Log.Info($"Initialized in {stopwatch.Elapsed.TotalSeconds:F2} seconds");
            stopwatch.Stop();
        }

        static void initConfigs()
        {
            // General
            {
                const string SECTION_NAME = "General";

                const string GUID = $"RoC_Config_{SECTION_NAME}";
                const string NAME = $"Risk of Chaos: {SECTION_NAME}";

                const string EFFECT_ACTIVACTION_MODE_NAME = "Effect Activation Mode";
                EffectActivationMode = Instance.Config.Bind(new ConfigDefinition(SECTION_NAME, EFFECT_ACTIVACTION_MODE_NAME), ChaosEffectMode.OnTimer, new ConfigDescription($"What should trigger a new effect\n\n{nameof(ChaosEffectMode.OncePerStage)}: A new effect is picked at the start of each stage\n\n{nameof(ChaosEffectMode.OnTimer)}: A new effect is picked on a timer"));
                // ModSettingsManager.AddOption(new ChoiceOption(EffectActivationMode), GUID, NAME);

                TimeBetweenEffects = Instance.Config.Bind(new ConfigDefinition(SECTION_NAME, "Effect Timer"), 60f, new ConfigDescription($"How often new effects should happen, only takes effect if '{EFFECT_ACTIVACTION_MODE_NAME}' is set to {nameof(ChaosEffectMode.OnTimer)}"));
                ModSettingsManager.AddOption(new StepSliderOption(TimeBetweenEffects, new StepSliderConfig
                {
                    formatString = "{0:F0}s",
                    increment = 1f,
                    min = 5f,
                    max = 60f * 5f,
                    checkIfDisabled = () => EffectActivationMode == null || EffectActivationMode.Value != ChaosEffectMode.OnTimer
                }), GUID, NAME);

                EffectOverlap = Instance.Config.Bind(new ConfigDefinition(SECTION_NAME, "Max Effect Overlap"), 1, new ConfigDescription("The maximum number of simultaneous effects"));
                ModSettingsManager.AddOption(new IntSliderOption(EffectOverlap, new IntSliderConfig
                {
                    min = 1,
                    max = 10
                }), GUID, NAME);

                // ModSettingsManager.SetModIcon(general_icon, GUID, NAME);
                ModSettingsManager.SetModDescription("General config options for Risk of Chaos", GUID, NAME);
            }
        }
    }
}
