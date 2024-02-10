using System.IO;
using UnityEngine;

namespace RiskOfChaos.Utilities.PersistentSaveData
{
    public static class PersistentSaveDataManager
    {
        public static readonly string DirectoryPath = Path.Combine(Application.persistentDataPath, Main.PluginName);

        public static string GetSaveFilePath(string fileName)
        {
            return Path.Combine(DirectoryPath, fileName);
        }

        static PersistentSaveDataManager()
        {
            if (!Directory.Exists(DirectoryPath))
            {
                Directory.CreateDirectory(DirectoryPath);

#if DEBUG
                Log.Debug($"Created persistent save data directory: {DirectoryPath}");
#endif
            }
        }
    }
}
