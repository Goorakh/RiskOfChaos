using System.IO;

namespace RiskOfChaos
{
    static class LanguageFileHandler
    {
        internal static void Init()
        {
            RoR2.Language.collectLanguageRootFolders += static folders =>
            {
                string languageFolderPath = Path.Combine(Path.GetDirectoryName(Main.Instance.Info.Location), "lang");
                if (Directory.Exists(languageFolderPath))
                {
#if DEBUG
                    Log.Debug($"Found lang folder at {languageFolderPath}, adding to list ({string.Join(", ", folders)})");
#endif

                    folders.Add(languageFolderPath);
                }
                else
                {
                    Log.Warning($"Unable to find lang folder at {languageFolderPath}");
                }
            };
        }
    }
}
