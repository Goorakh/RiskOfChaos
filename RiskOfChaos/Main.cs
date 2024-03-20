using BepInEx;
using HarmonyLib;
using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.ModCompatibility;
using RiskOfChaos.ModifierController;
using RiskOfChaos.Networking;
using RiskOfChaos.Utilities;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace RiskOfChaos
{
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInDependency(R2API.RecalculateStatsAPI.PluginGUID)]
    [BepInDependency(R2API.Networking.NetworkingAPI.PluginGUID)]
    [BepInDependency(R2API.PrefabAPI.PluginGUID)]
    [BepInDependency(R2API.DotAPI.PluginGUID)]
    [BepInDependency(R2API.DamageAPI.PluginGUID)]
    [BepInDependency("com.rune580.riskofoptions")]
    [BepInDependency(ProperSave.ProperSavePlugin.GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class Main : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Gorakh";
        public const string PluginName = "RiskOfChaos";
        public const string PluginVersion = "1.15.0";

        Harmony _harmonyInstance;

        public static string ModDirectory { get; private set; }

        public ContentPackProvider ContentPackProvider { get; private set; }

        void Awake()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            Log.Init(Logger);

            ModDirectory = Path.GetDirectoryName(Info.Location);

            ContentPackProvider = new ContentPackProvider();
            ContentPackProvider.Register();

            LanguageFileHandler.Init();

            NetworkMessageManager.RegisterMessages();

            NetPrefabs.InitializeAll();
            ValueModificationManagerInstantiator.Initialize();

            AdditionalResourceAvailability.InitHooks();

            if (ProperSaveCompat.Active)
            {
                ProperSaveCompat.Init();
            }

            _harmonyInstance = new Harmony("com." + PluginGUID);
            _harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());

            initConfigs();

            Log.Info_NoCallerPrefix($"Initialized in {stopwatch.Elapsed.TotalSeconds:F2} seconds");
            stopwatch.Stop();
        }

        void initConfigs()
        {
            Config.SaveOnConfigSet = false;

            Configs.Init(Config);

            ChaosEffectCatalog.InitConfig(Config);

            RoR2.RoR2Application.onLoad = (Action)Delegate.Combine(RoR2.RoR2Application.onLoad, () =>
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
            });
        }

        void OnDestroy()
        {
            if (ProperSaveCompat.Active)
            {
                ProperSaveCompat.Cleanup();
            }

            _harmonyInstance?.UnpatchSelf();
        }
    }
}
