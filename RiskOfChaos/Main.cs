using BepInEx;
using HarmonyLib;
using R2API.Utils;
using RiskOfChaos.Config;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.Networking;
using RiskOfChaos.Patches;
using System.Diagnostics;

namespace RiskOfChaos
{
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInDependency(R2API.RecalculateStatsAPI.PluginGUID)]
    [BepInDependency(R2API.Networking.NetworkingAPI.PluginGUID)]
    [BepInDependency(R2API.PrefabAPI.PluginGUID)]
    [BepInDependency("com.rune580.riskofoptions")]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class Main : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Gorakh";
        public const string PluginName = "RiskOfChaos";
        public const string PluginVersion = "0.9.0";

        internal static Main Instance { get; private set; }

        void Awake()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            Log.Init(Logger);

            Instance = this;

            ChaosEffectManager.InitializeObject();

            FixPlayerChatOnRespawn.Apply();

            LanguageFileHandler.Init();

            MidRunArtifactsHandler.InitListeners();

            NetworkMessageManager.RegisterMessages();

            NetPrefabs.InitializeAll();

            Harmony harmony = new Harmony(PluginGUID);
            harmony.PatchAll();

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
