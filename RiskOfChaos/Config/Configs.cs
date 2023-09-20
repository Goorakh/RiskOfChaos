using BepInEx.Configuration;
using RiskOfOptions;
using UnityEngine;

namespace RiskOfChaos
{
    public static partial class Configs
    {
        const string CONFIG_GUID = $"RoC_Config_General";
        const string CONFIG_NAME = $"Risk of Chaos: General";

        public static readonly Sprite GenericIcon;

        static Configs()
        {
            Texture2D genericIconTexture = new Texture2D(256, 256);
            if (genericIconTexture.LoadImage(Properties.Resources.icon))
            {
                GenericIcon = Sprite.Create(genericIconTexture, new Rect(0f, 0f, genericIconTexture.width, genericIconTexture.height), new Vector2(0.5f, 0.5f));
            }
            else
            {
                Log.Error("Failed to load config icon");
            }
        }

        internal static void Init(ConfigFile file)
        {
            General.Bind(file);

            UI.Bind(file);

            ChatVoting.Bind(file);

#if DEBUG
            Debug.Bind(file);
#endif

            if (GenericIcon)
            {
                ModSettingsManager.SetModIcon(GenericIcon, CONFIG_GUID, CONFIG_NAME);
            }

            ModSettingsManager.SetModDescription("General config options for Risk of Chaos", CONFIG_GUID, CONFIG_NAME);
        }
    }
}
