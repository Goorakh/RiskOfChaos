using BepInEx;
using HarmonyLib;
using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.ModCompatibility;
using RiskOfChaos.Networking;
using RiskOfChaos.Utilities;
using RoR2;
using System.Diagnostics;
using System.Reflection;

using Path = System.IO.Path;

namespace RiskOfChaos
{
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInDependency(R2API.RecalculateStatsAPI.PluginGUID)]
    [BepInDependency(R2API.Networking.NetworkingAPI.PluginGUID)]
    [BepInDependency(R2API.DamageAPI.PluginGUID)]
    [BepInDependency(R2API.ProcTypeAPI.PluginGUID)]
    [BepInDependency(RiskOfOptions.PluginInfo.PLUGIN_GUID)]
    [BepInDependency(ProperSave.ProperSavePlugin.GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public sealed class RiskOfChaosPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Gorakh";
        public const string PluginName = "RiskOfChaos";
        public const string PluginVersion = "2.7.0";

        Harmony _harmonyInstance;

        static RiskOfChaosPlugin _instance;
        public static RiskOfChaosPlugin Instance => _instance;

        public static string ModDirectory { get; private set; }

        public RoCContent ContentPackProvider { get; private set; }

        void Awake()
        {
            SingletonHelper.Assign(ref _instance, this);

            Stopwatch stopwatch = Stopwatch.StartNew();

            Log.Init(Logger);

            TaskExceptionHandler.Initialize();

            RiskOfTwitch.Log.LogSource = new TwitchLibLogSource();

            ModDirectory = Path.GetDirectoryName(Info.Location);

            ContentPackProvider = new RoCContent();
            ContentPackProvider.Register();

            LanguageFileHandler.Init();

            NetworkMessageManager.RegisterMessages();

            AdditionalResourceAvailability.InitHooks();

            if (ProperSaveCompat.Active)
            {
                ProperSaveCompat.Init();
            }

            ProcTypeAPICompat.Init();

            _harmonyInstance = new Harmony("com." + PluginGUID);
            _harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());

            initConfigs();

            Log.Message_NoCallerPrefix($"Initialized in {stopwatch.Elapsed.TotalSeconds:F2} seconds");
            stopwatch.Stop();
        }

        void initConfigs()
        {
            Config.SaveOnConfigSet = false;

            Configs.Init(Config);

            ChaosEffectCatalog.InitConfig(Config);

            RoR2Application.onLoad += () =>
            {
                Config.SaveOnConfigSet = true;

#if DEBUG
                Stopwatch stopwatch = Stopwatch.StartNew();
#endif

                Config.Save();

#if DEBUG
                Log.Debug($"Finished initializing config file (Written to file in {stopwatch.Elapsed.TotalMilliseconds:F0}ms)");
                stopwatch.Stop();
#endif

                Configs.Metadata.CheckVersion();
            };
        }

        void OnDestroy()
        {
            SingletonHelper.Unassign(ref _instance, this);

            TaskExceptionHandler.Cleanup();

            if (ProperSaveCompat.Active)
            {
                ProperSaveCompat.Cleanup();
            }

            _harmonyInstance?.UnpatchSelf();
        }
    }
}