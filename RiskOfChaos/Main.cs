using BepInEx;
using HarmonyLib;
using R2API.Utils;
using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.Networking;
using RiskOfChaos.Utilities;
using System.Diagnostics;
using System.IO;

namespace RiskOfChaos
{
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInDependency(R2API.RecalculateStatsAPI.PluginGUID)]
    [BepInDependency(R2API.Networking.NetworkingAPI.PluginGUID)]
    [BepInDependency(R2API.PrefabAPI.PluginGUID)]
    [BepInDependency(R2API.DamageAPI.PluginGUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.rune580.riskofoptions")]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class Main : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Gorakh";
        public const string PluginName = "RiskOfChaos";
        public const string PluginVersion = "1.12.1";

        internal static string ModDirectory { get; private set; }

        public ContentPackProvider ContentPackProvider;

        void Awake()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            Log.Init(Logger);

            ModDirectory = Path.GetDirectoryName(Info.Location);

            ContentPackProvider = new ContentPackProvider();

            LanguageFileHandler.Init();

            NetworkMessageManager.RegisterMessages();

            NetPrefabs.InitializeAll();

            AdditionalResourceAvailability.InitHooks();

            Harmony harmony = new Harmony(PluginGUID);
            harmony.PatchAll();

            initConfigs();

            Log.Info_NoCallerPrefix($"Initialized in {stopwatch.Elapsed.TotalSeconds:F2} seconds");
            stopwatch.Stop();
        }

        void initConfigs()
        {
            Configs.Init(Config);

            ChaosEffectCatalog.InitConfig(Config);
        }
    }
}
