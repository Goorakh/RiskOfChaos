using System.IO;
using UnityEngine;

namespace RiskOfChaos.Utilities.PersistentSaveData
{
    public static class PersistentSaveDataManager
    {
        static string _directoryPath = null;
        public static string DirectoryPath
        {
            get
            {
                if (_directoryPath == null)
                {
                    _directoryPath = Path.Combine(Application.persistentDataPath, RiskOfChaosPlugin.PluginName);

                    if (!Directory.Exists(_directoryPath))
                    {
                        Directory.CreateDirectory(_directoryPath);
                        Log.Debug($"Created persistent save data directory: {_directoryPath}");
                    }
                }

                return _directoryPath;
            }
        }

        public static string GetSaveFilePath(string fileName)
        {
            return Path.Combine(DirectoryPath, fileName);
        }
    }
}
