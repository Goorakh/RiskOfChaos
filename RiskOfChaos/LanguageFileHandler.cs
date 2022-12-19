using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RiskOfChaos
{
    static class LanguageFileHandler
    {
        internal static void Init()
        {
            RoR2.Language.collectLanguageRootFolders += static folders =>
            {
                const string LOG_PREFIX = $"{nameof(LanguageFileHandler)}.{nameof(RoR2.Language.collectLanguageRootFolders)}";

                string languageFolderPath = Path.Combine(Path.GetDirectoryName(Main.Instance.Info.Location), "lang");
                if (Directory.Exists(languageFolderPath))
                {
#if DEBUG
                    Log.Debug(LOG_PREFIX + $"Found lang folder at {languageFolderPath}, adding to list ({string.Join(", ", folders)})");
#endif

                    folders.Add(languageFolderPath);
                }
                else
                {
                    Log.Warning(LOG_PREFIX + $"Unable to find lang folder at {languageFolderPath}");
                }
            };
        }
    }
}
