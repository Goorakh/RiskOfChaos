using BepInEx.Configuration;
using RiskOfChaos.ConfigHandling;
using RiskOfOptions.OptionConfigs;
using UnityEngine;

namespace RiskOfChaos
{
    partial class Configs
    {
        public static class UI
        {
            public const string SECTION_NAME = "UI";

            public static readonly ConfigHolder<bool> HideActiveEffectsPanel =
                ConfigFactory<bool>.CreateConfig("Hide Active Effects Panel", false)
                                   .Description("Hides the active effects list under the Objectives display")
                                   .OptionConfig(new CheckBoxConfig())
                                   .Build();

            static bool activeEffectsPanelHidden() => HideActiveEffectsPanel.Value;

            public static readonly ConfigHolder<Color> ActiveEffectsTextColor =
                ConfigFactory<Color>.CreateConfig("Active Effect Text Color", Color.white)
                                    .Description("The color of the effect names in the \"Active Effects\" list")
                                    .OptionConfig(new ColorOptionConfig
                                    {
                                        checkIfDisabled = activeEffectsPanelHidden
                                    })
                                    .MovedFrom(General.SECTION_NAME)
                                    .Build();

            public static readonly ConfigHolder<bool> DisplayNextEffect =
                ConfigFactory<bool>.CreateConfig("Display Next Effect", true)
                                   .Description("Displays the next effect that will happen.\nOnly works if chat voting is disabled and seeded mode is enabled")
                                   .OptionConfig(new CheckBoxConfig())
                                   .Build();

            internal static void Bind(ConfigFile file)
            {
                void bindConfig<T>(ConfigHolder<T> config)
                {
                    config.Bind(file, SECTION_NAME, CONFIG_GUID, CONFIG_NAME);
                }

                bindConfig(HideActiveEffectsPanel);

                bindConfig(ActiveEffectsTextColor);

                bindConfig(DisplayNextEffect);
            }
        }
    }
}
