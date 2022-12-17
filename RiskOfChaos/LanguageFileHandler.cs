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
            const string LOG_PREFIX = $"{nameof(LanguageFileHandler)}.{nameof(Language_SetFolders)} ";

            string languageFolderPath = Path.Combine(Path.GetDirectoryName(Main.Instance.Info.Location), "lang");
            if (Directory.Exists(languageFolderPath))
            {
                const string DEFAULT_LANG_NAME = "en";

                IEnumerable<string> dirs = Directory.EnumerateDirectories(languageFolderPath, self.name);
                if (!dirs.Any())
                {
                    dirs = Directory.EnumerateDirectories(languageFolderPath, DEFAULT_LANG_NAME);

                    Log.Info(LOG_PREFIX + $"did not find lang folder for {self.name} ({languageFolderPath}), defaulting to {DEFAULT_LANG_NAME}, dirs=[{string.Join(", ", dirs)}]");
                }
                else
                {
                    Log.Info(LOG_PREFIX + $"found lang folder for {self.name} ({languageFolderPath}), dirs=[{string.Join(", ", dirs)}]");
                }

                orig(self, newFolders.Union(dirs));
                return;
            }

            Log.Warning(LOG_PREFIX + $"Unable to find lang folder for {self.name}, path: {languageFolderPath}");

            orig(self, newFolders);
        }
    }
}
