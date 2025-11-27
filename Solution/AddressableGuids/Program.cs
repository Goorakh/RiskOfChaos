using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

sealed class Program
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

        using (TextReader textReader = new StreamReader(addressablesListFilePath))
        using (JsonReader reader = new JsonTextReader(textReader))
        {
            JObject jObject = (JObject)JToken.ReadFrom(reader);

            outputBuilder.AppendLine("public static class AddressableGuids");
            outputBuilder.AppendLine("{");

            foreach ((string propertyName, JToken propertyValue) in jObject)
            {
                if (propertyValue is JValue value)
                {
                    if (value.Type == JTokenType.String)
                    {
                        outputBuilder.AppendLine($"\t/// <summary>");
                        outputBuilder.AppendLine($"\t/// {propertyName}");
                        outputBuilder.AppendLine($"\t/// </summary>");
                        outputBuilder.AppendLine($"\tpublic const string {filterFieldName(propertyName)} = \"{(string)propertyValue}\";");
                    }
                    else
                    {
                        throw new NotImplementedException($"Value type {value.Type} is not implemented");
                    }
                }
            }

            outputBuilder.AppendLine("}");

            outputBuilder.AppendLine("public static class WwiseData");
            outputBuilder.AppendLine("{");

            HashSet<string> addedWwiseNames = new HashSet<string>();

            foreach ((string propertyName, JToken propertyValue) in jObject)
            {
                if (!propertyName.StartsWith("Wwise/") || !propertyName.EndsWith("_data"))
                    continue;

                string eventPropertyName = propertyName.Remove(propertyName.Length - 5);
                if (!(jObject[eventPropertyName] is JValue eventGuid) || eventGuid.Type != JTokenType.String)
                    throw new Exception("this shit is wrong");

                if (!(propertyValue is JObject obj))
                    throw new Exception("Unexpected json token type");

                JValue name = obj["name"] as JValue;
                JValue wwiseId = obj["WWiseID"] as JValue;
                if (name == null || name.Type != JTokenType.String || wwiseId == null || wwiseId.Type != JTokenType.Integer)
                    throw new Exception("missing wwise object properties");

                string className = filterFieldName((string)name);
                if (!addedWwiseNames.Add(className))
                {
                    Console.WriteLine($"WARN: Duplicate wwise event class name {(string)name} ({className})");
                    continue;
                }

                outputBuilder.AppendLine($"\tpublic static class {className}");
                outputBuilder.AppendLine("\t{");

                outputBuilder.AppendLine($"\t\tpublic const string AddressableGuid = \"{(string)eventGuid}\";");
                outputBuilder.AppendLine($"\t\tpublic const uint WWiseID = {(uint)wwiseId};");

                outputBuilder.AppendLine("\t}");
            }

            outputBuilder.AppendLine("}");
        }

        File.WriteAllText(destinationFile, outputBuilder.ToString());
    }

    static string filterFieldName(string name)
    {
        StringBuilder fieldNameBuilder = new StringBuilder();
        foreach (char c in name)
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

        return fieldNameBuilder.ToString();
    }
}

static class Extensions
{
    public static void Deconstruct<TKey, TValue>(this in KeyValuePair<TKey, TValue> kvp, out TKey key, out TValue value)
    {
        key = kvp.Key;
        value = kvp.Value;
    }
}
