using RiskOfChaos.ModCompatibility;
using RiskOfChaos.SaveHandling.DataContainers;

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

        public static bool IsSaveDataLoaded => ProperSaveCompat.Active && ProperSaveCompat.LoadingComplete;

        public static bool UseSaveData => ProperSaveCompat.Active;

        internal static SaveContainer CollectAllSaveData()
        {
            if (!UseSaveData)
                return null;

            SaveContainer container = SaveContainer.CreateEmpty();
            CollectSaveData?.Invoke(ref container);
            return container;
        }

        internal static void OnSaveDataLoaded(SaveContainer saveContainer)
        {
            if (!UseSaveData || saveContainer is null)
                return;

            _currentSaveContainer = saveContainer;
            _loadSaveData?.Invoke(saveContainer);
        }
    }
}
