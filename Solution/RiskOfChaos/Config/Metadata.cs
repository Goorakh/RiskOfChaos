using BepInEx.Configuration;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.Utilities;
using RoR2.UI;
using System.Linq;

namespace RiskOfChaos
{
    partial class Configs
    {
        internal static class Metadata
        {
            public const string SECTION_NAME = "META";

            public const uint CONFIG_FILE_VERSION_LEGACY = 0;
            public const uint CURRENT_CONFIG_FILE_VERSION = 18;

            public static ConfigHolder<uint> ConfigFileVersion =
                ConfigFactory<uint>.CreateConfig("VERSION", CONFIG_FILE_VERSION_LEGACY)
                                   .Description("""
                                    Used internally by the mod
                                    DO NOT MODIFY MANUALLY
                                    """)
                                   .Build();

            internal static void Bind(ConfigFile file)
            {
                void bindConfig(ConfigHolderBase config)
                {
                    config.Bind(file, SECTION_NAME, CONFIG_GUID, CONFIG_NAME);
                }

                bindConfig(ConfigFileVersion);
            }

            static bool isOutdatedVersion()
            {
                // == 0 will always be < whatever current version is, so don't need to check it
                if (ConfigFileVersion.Value < CURRENT_CONFIG_FILE_VERSION)
                {
                    return ConfigManager.AllConfigs.Any(c => !c.IsDefaultValue);
                }
                else
                {
                    return false;
                }
            }

            internal static void CheckVersion()
            {
                if (isOutdatedVersion())
                {
                    PopupAlertQueue.EnqueueAlert(dialogBox =>
                    {
                        dialogBox.headerToken = new SimpleDialogBox.TokenParamsPair("POPUP_CONFIG_UPDATE_HEADER");
                        dialogBox.descriptionToken = new SimpleDialogBox.TokenParamsPair("POPUP_CONFIG_UPDATE_DESCRIPTION");

                        dialogBox.AddCommandButton("roc_delete_config", "POPUP_CONFIG_UPDATE_RESET");

                        dialogBox.AddCancelButton("POPUP_CONFIG_UPDATE_IGNORE");
                    });
                }

                ConfigFileVersion.LocalValue = CURRENT_CONFIG_FILE_VERSION;
            }
        }
    }
}
