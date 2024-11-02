using BepInEx.Bootstrap;
using ProperSave;
using RiskOfChaos.SaveHandling;
using RoR2;
using System;
using System.Collections.Generic;

namespace RiskOfChaos.ModCompatibility
{
    static class ProperSaveCompat
    {
        public static bool Active => Chainloader.PluginInfos.ContainsKey(ProperSavePlugin.GUID);

        public static bool LoadingComplete { get; private set; }

        const string SAVE_DATA_KEY = $"{Main.PluginGUID}";

        public static void Init()
        {
            SaveFile.OnGatherSaveData += SaveFile_OnGatherSaveData;
            Loading.OnLoadingEnded += Loading_OnLoadingEnded;

            Run.onRunDestroyGlobal += Run_onRunDestroyGlobal;
        }

        public static void Cleanup()
        {
            SaveFile.OnGatherSaveData -= SaveFile_OnGatherSaveData;
            Loading.OnLoadingEnded -= Loading_OnLoadingEnded;

            Run.onRunDestroyGlobal -= Run_onRunDestroyGlobal;
        }

        static void SaveFile_OnGatherSaveData(Dictionary<string, object> saveDataDict)
        {
            string saveData = SaveManager.CollectAllSaveData();
            if (string.IsNullOrEmpty(saveData))
                return;

            if (saveDataDict.ContainsKey(SAVE_DATA_KEY))
            {
                Log.Warning($"Save data Key {SAVE_DATA_KEY} already added");
            }
            else
            {
                saveDataDict.Add(SAVE_DATA_KEY, saveData);

#if DEBUG
                Log.Debug($"Added save data with key '{SAVE_DATA_KEY}'");
#endif
            }
        }

        static void Loading_OnLoadingEnded(SaveFile saveFile)
        {
            string saveData;
            try
            {
                saveData = saveFile.GetModdedData<string>(SAVE_DATA_KEY);
            }
            catch (KeyNotFoundException) // Save data doesn't exist, ignore
            {
                return;
            }
            catch (Exception e)
            {
                Log.Error_NoCallerPrefix($"Failed to load save data: {e}");
                return;
            }

#if DEBUG
            Log.Debug($"Loaded save data with key '{SAVE_DATA_KEY}'");
#endif

            SaveManager.OnSaveDataLoaded(saveData);
            LoadingComplete = true;
        }

        static void Run_onRunDestroyGlobal(Run obj)
        {
            LoadingComplete = false;
        }
    }
}
