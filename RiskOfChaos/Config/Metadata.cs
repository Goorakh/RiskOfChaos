﻿using BepInEx.Configuration;
using RiskOfChaos.ConfigHandling;
using RoR2.UI;
using RoR2.UI.MainMenu;
using System.Linq;

namespace RiskOfChaos
{
    partial class Configs
    {
        internal static class Metadata
        {
            const string SECTION_NAME = "META";

            public const uint CONFIG_FILE_VERSION_LEGACY = 0;
            public const uint CURRENT_CONFIG_FILE_VERSION = 4;

            public static ConfigHolder<uint> ConfigFileVersion =
                ConfigFactory<uint>.CreateConfig("VERSION", CONFIG_FILE_VERSION_LEGACY)
                                   .Description("Used internally by the mod\nDO NOT MODIFY MANUALLY")
                                   .Build();

            internal static void Bind(ConfigFile file)
            {
                void bindConfig<T>(ConfigHolder<T> config)
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
                    return ConfigMonitor.AllConfigs.Any(c => !c.IsDefaultValue);
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
                    void BaseMainMenuScreen_OnEnter(On.RoR2.UI.MainMenu.BaseMainMenuScreen.orig_OnEnter orig, BaseMainMenuScreen self, MainMenuController mainMenuController)
                    {
                        orig(self, mainMenuController);

                        if (self == mainMenuController.titleMenuScreen)
                        {
                            On.RoR2.UI.MainMenu.BaseMainMenuScreen.OnEnter -= BaseMainMenuScreen_OnEnter;

                            SimpleDialogBox dialogBox = SimpleDialogBox.Create();
                            dialogBox.headerToken = new SimpleDialogBox.TokenParamsPair("POPUP_CONFIG_UPDATE_HEADER");
                            dialogBox.descriptionToken = new SimpleDialogBox.TokenParamsPair("POPUP_CONFIG_UPDATE_DESCRIPTION");

                            dialogBox.AddActionButton(() =>
                            {
                                foreach (ConfigHolderBase config in ConfigMonitor.AllConfigs)
                                {
                                    if (config.Entry.Definition.Section == SECTION_NAME)
                                        continue;

#if DEBUG
                                    if (!config.IsDefaultValue)
                                    {
                                        Log.Debug($"Reset config value: {config.Entry.Definition}");
                                    }
#endif

                                    config.LocalBoxedValue = config.Entry.DefaultValue;
                                }
                            }, "POPUP_CONFIG_UPDATE_RESET");

                            dialogBox.AddCancelButton("POPUP_CONFIG_UPDATE_IGNORE");
                        }
                    }

                    On.RoR2.UI.MainMenu.BaseMainMenuScreen.OnEnter += BaseMainMenuScreen_OnEnter;
                }

                ConfigFileVersion.LocalValue = CURRENT_CONFIG_FILE_VERSION;
            }
        }
    }
}
