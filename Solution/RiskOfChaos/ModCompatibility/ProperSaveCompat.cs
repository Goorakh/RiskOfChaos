using BepInEx.Bootstrap;
using ProperSave;
using RiskOfChaos.SaveHandling;
using RiskOfChaos.SaveHandling.DataContainers;
using RoR2;
using System.Collections.Generic;

namespace RiskOfChaos.ModCompatibility
{
    static class ProperSaveCompat
    {
        public static bool Active => Chainloader.PluginInfos.ContainsKey(ProperSavePlugin.GUID);

        public static bool LoadingComplete { get; private set; }

        const string SAVE_DATA_KEY = $"{Main.PluginGUID}_SaveData";

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
            SaveContainer saveContainer = SaveManager.CollectAllSaveData();
            if (saveContainer is null)
                return;

            if (saveDataDict.ContainsKey(SAVE_DATA_KEY))
            {
                Log.Warning($"Save data Key {SAVE_DATA_KEY} already added");
            }
            else
            {
                saveDataDict.Add(SAVE_DATA_KEY, saveContainer);
            }
        }

        static void Loading_OnLoadingEnded(SaveFile saveFile)
        {
            SaveContainer saveContainer;
            try
            {
                saveContainer = saveFile.GetModdedData<SaveContainer>(SAVE_DATA_KEY);
            }
            catch (KeyNotFoundException) // Save data doesn't exist, ignore
            {
                return;
            }

            SaveManager.OnSaveDataLoaded(saveContainer);
            LoadingComplete = true;
        }

        static void Run_onRunDestroyGlobal(Run obj)
        {
            LoadingComplete = false;
        }
    }
}
