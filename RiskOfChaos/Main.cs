using BepInEx;
using R2API.Utils;
using RiskOfChaos.Config;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.Networking;
using System.Diagnostics;

namespace RiskOfChaos
{
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInDependency(R2API.RecalculateStatsAPI.PluginGUID)]
    [BepInDependency(R2API.Networking.NetworkingAPI.PluginGUID)]
    [BepInDependency("com.rune580.riskofoptions")]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class Main : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Gorakh";
        public const string PluginName = "RiskOfChaos";
        public const string PluginVersion = "0.6.0";

        internal static Main Instance { get; private set; }

        void Awake()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            Log.Init(Logger);

            Instance = this;

            LanguageFileHandler.Init();

            MidRunArtifactsHandler.PatchEnemyInfoPanel();

            NetworkMessageManager.RegisterMessages();

            initConfigs();

            Log.Info_NoCallerPrefix($"Initialized in {stopwatch.Elapsed.TotalSeconds:F2} seconds");
            stopwatch.Stop();
        }

        void initConfigs()
        {
            Configs.General.Init(Config);

            ChaosEffectCatalog.InitConfig();
        }
    }
}
