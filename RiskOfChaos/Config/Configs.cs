using BepInEx.Configuration;
using RiskOfOptions;
using System;
using System.IO;
using UnityEngine;

namespace RiskOfChaos
{
    public static partial class Configs
    {
        const string CONFIG_GUID = $"RoC_Config_General";
        const string CONFIG_NAME = $"Risk of Chaos: General";

        public static Sprite GenericIcon { get; private set; }

        static void findIcon()
        {
            FileInfo iconFile = null;

            DirectoryInfo dir = new DirectoryInfo(Main.ModDirectory);
            do
            {
                FileInfo[] files = dir.GetFiles("icon.png", SearchOption.TopDirectoryOnly);
                if (files != null && files.Length > 0)
                {
                    iconFile = files[0];
                    break;
                }

                dir = dir.Parent;

            } while (dir != null && !string.Equals(dir.Name, "plugins", StringComparison.OrdinalIgnoreCase));

            if (iconFile != null)
            {
                Texture2D iconTexture = new Texture2D(256, 256);
                if (iconTexture.LoadImage(File.ReadAllBytes(iconFile.FullName)))
                {
                    GenericIcon = Sprite.Create(iconTexture, new Rect(0f, 0f, iconTexture.width, iconTexture.height), new Vector2(0.5f, 0.5f));
                }
            }

            if (!GenericIcon)
            {
                Log.Error("Failed to load config icon");
            }
        }

        internal static void Init(ConfigFile file)
        {
            General.Bind(file);

            EffectSelection.Bind(file);

            UI.Bind(file);

            ChatVoting.Bind(file);

            TwitchVoting.Bind(file);

#if DEBUG
            Debug.Bind(file);
#endif

            Metadata.Bind(file);

            findIcon();

            if (GenericIcon)
            {
                ModSettingsManager.SetModIcon(GenericIcon, CONFIG_GUID, CONFIG_NAME);
            }

            ModSettingsManager.SetModDescription("General config options for Risk of Chaos", CONFIG_GUID, CONFIG_NAME);
        }
    }
}
