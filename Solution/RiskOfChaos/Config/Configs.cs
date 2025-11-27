using BepInEx.Configuration;
using RiskOfOptions;
using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RiskOfChaos
{
    public static partial class Configs
    {
        const string CONFIG_GUID = $"RoC_Config_General";
        const string CONFIG_NAME = $"Risk of Chaos: General";

        public static Sprite GenericIcon { get; private set; }

        static FileInfo findFileInParentDirectories(DirectoryInfo startDir, string searchPattern)
        {
            for (DirectoryInfo dir = startDir; dir != null; dir = dir.Parent)
            {
                if (string.Equals(dir.FullName, BepInEx.Paths.PluginPath, StringComparison.OrdinalIgnoreCase))
                {
                    Log.Debug($"File search (matching '{searchPattern}') reached plugin directory");
                    break;
                }

                Log.Debug($"Searching '{dir.FullName}' for file matching '{searchPattern}'");

                FileInfo iconFile = dir.EnumerateFiles(searchPattern, SearchOption.TopDirectoryOnly).FirstOrDefault();
                if (iconFile != null)
                {
                    Log.Debug($"File matching '{searchPattern}' found at: {iconFile.FullName}");
                    return iconFile;
                }
            }

            return null;
        }

        static Sprite generateIcon()
        {
            DirectoryInfo pluginDir = new DirectoryInfo(RiskOfChaosPlugin.ModDirectory);
            FileInfo iconFile = findFileInParentDirectories(pluginDir, "icon.png");
            if (iconFile == null)
            {
                return null;
            }

            Texture2D iconTexture = new Texture2D(256, 256);
            iconTexture.name = $"tex{RiskOfChaosPlugin.PluginName}Icon";
            if (!iconTexture.LoadImage(File.ReadAllBytes(iconFile.FullName)))
            {
                GameObject.Destroy(iconTexture);
                return null;
            }

            Sprite icon = Sprite.Create(iconTexture, new Rect(0f, 0f, iconTexture.width, iconTexture.height), new Vector2(0.5f, 0.5f));
            icon.name = $"{RiskOfChaosPlugin.PluginName}Icon";
            return icon;
        }

        internal static void Init(ConfigFile file)
        {
            General.Bind(file);

            EffectSelection.Bind(file);

            UI.Bind(file);

            ChatVoting.Bind(file);

            ChatVotingUI.Bind(file);

#if DEBUG
            Debug.Bind(file);
#endif

            Metadata.Bind(file);

            if (!GenericIcon)
            {
                GenericIcon = generateIcon();
            }

            if (GenericIcon)
            {
                ModSettingsManager.SetModIcon(GenericIcon, CONFIG_GUID, CONFIG_NAME);
            }

            ModSettingsManager.SetModDescription("General config options for Risk of Chaos", CONFIG_GUID, CONFIG_NAME);
        }
    }
}
