using BepInEx;
using BepInEx.Configuration;
using R2API.Utils;
using RiskOfChaos.EffectHandling;
using RiskOfOptions;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using System.Diagnostics;

namespace RiskOfChaos
{
    [NetworkCompatibility(CompatibilityLevel.NoNeedForSync, VersionStrictness.DifferentModVersionsAreOk)]
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInDependency(R2API.Networking.NetworkingAPI.PluginGUID)]
    [BepInDependency(R2API.RecalculateStatsAPI.PluginGUID)]
    [BepInDependency("com.rune580.riskofoptions")]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class Main : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Gorakh";
        public const string PluginName = "RiskOfChaos";
        public const string PluginVersion = "0.1.7";

        internal static Main Instance;

        public static ConfigEntry<float> TimeBetweenEffects { get; private set; }

        void Awake()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            Log.Init(Logger);

            Instance = this;

            LanguageFileHandler.Init();

            MidRunArtifactsHandler.PatchEnemyInfoPanel();

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

                TimeBetweenEffects = Instance.Config.Bind(new ConfigDefinition(SECTION_NAME, "Effect Timer"), 60f, new ConfigDescription($"How often new effects should happen"));
                ModSettingsManager.AddOption(new StepSliderOption(TimeBetweenEffects, new StepSliderConfig
                {
                    formatString = "{0:F0}s",
                    increment = 1f,
                    min = 5f,
                    max = 60f * 5f
                }), GUID, NAME);

                // ModSettingsManager.SetModIcon(general_icon, GUID, NAME);
                ModSettingsManager.SetModDescription("General config options for Risk of Chaos", GUID, NAME);
            }

            ChaosEffectCatalog.InitConfig();
        }
    }
}
