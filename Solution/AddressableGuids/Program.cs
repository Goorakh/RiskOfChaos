using Microsoft.Win32;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

class Program
{
    const string SteamRegistryPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Valve\Steam";

    const uint Ror2AppId = 632360U;

    static bool tryFindAppSteamDirectory(uint appId, out string appSteamDirectory)
    {
        string steamInstallPath = Registry.GetValue(SteamRegistryPath, "InstallPath", null)?.ToString();
        if (string.IsNullOrEmpty(steamInstallPath))
        {
            Console.WriteLine("Failed to find Steam install path");
            appSteamDirectory = null;
            return false;
        }

        string libraryFoldersPath = Path.Combine(steamInstallPath, "steamapps", "libraryfolders.vdf");
        if (!File.Exists(libraryFoldersPath))
        {
            Console.WriteLine($"File '{libraryFoldersPath}' not found");
            appSteamDirectory = null;
            return false;
        }

        string currentPath = null;
        foreach (string line in File.ReadAllLines(libraryFoldersPath))
        {
            Match pathMatch = Regex.Match(line, "\"path\"\\s+\"(.+?)\"");
            if (pathMatch.Success)
            {
                currentPath = pathMatch.Groups[1].Value;
                continue;
            }

            Match appEntryMatch = Regex.Match(line, $"\"{appId}\"\\s+\"\\d+?\"");
            if (appEntryMatch.Success)
            {
                if (currentPath != null)
                {
                    appSteamDirectory = currentPath;
                    return true;
                }
            }
        }

        appSteamDirectory = null;
        return false;
    }

    static bool tryFindAppInstallDirectory(uint appId, out string appInstallDirectory)
    {
        if (!tryFindAppSteamDirectory(appId, out string appSteamDirectory))
        {
            appInstallDirectory = null;
            return false;
        }

        string appManifestPath = Path.Combine(appSteamDirectory, "steamapps", $"appmanifest_{appId}.acf");
        if (!File.Exists(appManifestPath))
        {
            Console.WriteLine($"Failed to find app manifest at '{appManifestPath}'");

            appInstallDirectory = null;
            return false;
        }

        string appManifest = File.ReadAllText(appManifestPath);

        Match installDirMatch = Regex.Match(appManifest, "\"installdir\"\\s+\"(.+?)\"");
        if (!installDirMatch.Success)
        {
            Console.WriteLine($"Failed to find installdir in manifest '{appManifestPath}'");

            appInstallDirectory = null;
            return false;
        }

        string installDir = installDirMatch.Groups[1].Value;
        appInstallDirectory = Path.Combine(appSteamDirectory, "steamapps", "common", installDir);
        return true;
    }

    static string getCurrentFilePath([CallerFilePath] string filePath = "")
    {
        return filePath;
    }

    static void Main(string[] args)
    {
        if (!tryFindAppInstallDirectory(Ror2AppId, out string ror2AppInstallPath))
            return;

        string addressablesListFilePath = Path.Combine(ror2AppInstallPath, "Risk of Rain 2_Data", "StreamingAssets", "lrapi_returns.json");

        string destinationFile = Path.Combine(Path.GetDirectoryName(getCurrentFilePath()), "AddressableGuids.cs");

        StringBuilder outputBuilder = new StringBuilder();

        outputBuilder.AppendLine("public static class AddressableGuids");
        outputBuilder.AppendLine("{");

        foreach (string line in File.ReadAllLines(addressablesListFilePath))
        {
            Match match = Regex.Match(line, "\"(.+?)\"\\s*:\\s*\"(.+?)\"");
            if (!match.Success)
                continue;

            string assetName = match.Groups[1].Value;
            string assetGuid = match.Groups[2].Value;

            StringBuilder fieldNameBuilder = new StringBuilder();
            foreach (char c in assetName)
            {
                if (char.IsLetterOrDigit(c) || c == '_')
                {
                    if (fieldNameBuilder.Length == 0 && char.IsDigit(c))
                    {
                        fieldNameBuilder.Append('_');
                    }

                    fieldNameBuilder.Append(c);
                }
                else if (fieldNameBuilder[fieldNameBuilder.Length - 1] != '_')
                {
                    fieldNameBuilder.Append('_');
                }
            }

            outputBuilder.AppendLine($"\tpublic const string {fieldNameBuilder} = \"{assetGuid}\";");
        }

        outputBuilder.AppendLine("}");

        File.WriteAllText(destinationFile, outputBuilder.ToString());
    }
}