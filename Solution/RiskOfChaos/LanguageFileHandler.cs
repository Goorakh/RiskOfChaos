using System.IO;

namespace RiskOfChaos
{
    static class LanguageFileHandler
    {
        internal static void Init()
        {
            RoR2.Language.collectLanguageRootFolders += static folders =>
            {
                string languageFolderPath = Path.Combine(Main.ModDirectory, "lang");
                if (Directory.Exists(languageFolderPath))
                {
                    Log.Debug($"Found lang folder at {languageFolderPath}, adding to list ({string.Join(", ", folders)})");

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
