using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RiskOfChaos
{
    static class LanguageFileHandler
    {
        internal static void Init()
        {
            On.RoR2.Language.SetFolders += Language_SetFolders;
        }

        static void Language_SetFolders(On.RoR2.Language.orig_SetFolders orig, RoR2.Language self, IEnumerable<string> newFolders)
        {
            string languageFolderPath = Path.Combine(Path.GetDirectoryName(Main.Instance.Info.Location), "lang");
            Log.Debug(languageFolderPath);
            if (Directory.Exists(languageFolderPath))
            {
                IEnumerable<string> dirs = Directory.EnumerateDirectories(Path.Combine(languageFolderPath), self.name);
                orig(self, newFolders.Union(dirs));
                return;
            }

            orig(self, newFolders);
        }
    }
}
