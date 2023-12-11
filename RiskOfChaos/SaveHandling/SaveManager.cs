using RiskOfChaos.ModCompatibility;
using RiskOfChaos.SaveHandling.DataContainers;
using System;

namespace RiskOfChaos.SaveHandling
{
    static class SaveManager
    {
        public delegate void CollectSaveDataDelegate(ref SaveContainer container);
        public static event CollectSaveDataDelegate CollectSaveData;

        public delegate void OnSaveDataLoadedDelegate(in SaveContainer container);
        static event OnSaveDataLoadedDelegate _loadSaveData;
        public static event OnSaveDataLoadedDelegate LoadSaveData
        {
            add
            {
                _loadSaveData += value;

                // If data is already loaded, invoke the event immediately
                if (IsSaveDataLoaded && _currentSaveContainer is not null)
                {
                    value?.Invoke(_currentSaveContainer);
                }
            }
            remove
            {
                _loadSaveData -= value;
            }
        }

        static SaveContainer _currentSaveContainer;

        public static bool IsSaveDataLoaded => UseSaveData && ProperSaveCompat.LoadingComplete;

        public static bool UseSaveData => ProperSaveCompat.Active;

        public static bool IsCollectingSaveData { get; private set; }

        internal static SaveContainer CollectAllSaveData()
        {
            if (!UseSaveData)
                return null;

            SaveContainer container = new SaveContainer();

            IsCollectingSaveData = true;
            try
            {
                CollectSaveData?.Invoke(ref container);
            }
            catch (Exception e)
            {
                Log.Error_NoCallerPrefix($"Caught exception while collecting save data, nothing will be saved: {e}");
                return null;
            }
            finally
            {
                IsCollectingSaveData = false;
            }

#if DEBUG
            Log.Debug("Collected save data");
#endif

            return container;
        }

        internal static void OnSaveDataLoaded(SaveContainer saveContainer)
        {
            if (!UseSaveData || saveContainer is null)
                return;

            _currentSaveContainer = saveContainer;
            _loadSaveData?.Invoke(saveContainer);

#if DEBUG
            Log.Debug("Loaded save file");
#endif
        }
    }
}
