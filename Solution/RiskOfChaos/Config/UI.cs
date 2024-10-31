using BepInEx.Configuration;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.Trackers;
using RiskOfOptions.OptionConfigs;
using RoR2;
using RoR2.UI;
using System;
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

            public static readonly ConfigHolder<bool> DisplayAlwaysActiveEffects =
                ConfigFactory<bool>.CreateConfig("Display Permanently Active Effects", false)
                                   .Description("If effects configured to always be active should be displayed in the active effects panel")
                                   .OptionConfig(new CheckBoxConfig
                                   {
                                       checkIfDisabled = activeEffectsPanelHidden
                                   })
                                   .Build();

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
                                   .Description("""
                                    Displays the next effect that will happen.
                                    Only works if chat voting is disabled and seeded mode is enabled
                                    """)
                                   .OptionConfig(new CheckBoxConfig())
                                   .Build();

            public enum NextEffectTimerDisplayType : byte
            {
                Never,
                WhenRunTimerUnavailable,
                Always
            }

            public static readonly ConfigHolder<NextEffectTimerDisplayType> NextEffectTimerDisplayMode =
                ConfigFactory<NextEffectTimerDisplayType>.CreateConfig("Next Effect Timer Display Mode", NextEffectTimerDisplayType.WhenRunTimerUnavailable)
                                                         .Description($"""
                                                          Displays how much time is left until the next effect.
                                                          
                                                          {nameof(NextEffectTimerDisplayType.Never)}: The time remaining is never displayed.
                                                          {nameof(NextEffectTimerDisplayType.WhenRunTimerUnavailable)}: Displays time remaining only when the regular run timer is paused or otherwise not visible.
                                                          {nameof(NextEffectTimerDisplayType.Always)}: Time remaining is always displayed
                                                          """)
                                                         .OptionConfig(new ChoiceConfig())
                                                         .Build();

            public static bool ShouldShowNextEffectTimer(HUD hud)
            {
                switch (NextEffectTimerDisplayMode.Value)
                {
                    case NextEffectTimerDisplayType.Never:
                        return false;
                    case NextEffectTimerDisplayType.WhenRunTimerUnavailable:
                        Run run = Run.instance;
                        return run && ((run.isRunStopwatchPaused && General.RunEffectsTimerWhileRunTimerPaused.Value) || !RunTimerUITracker.IsAnyTimerVisibleForHUD(hud));
                    case NextEffectTimerDisplayType.Always:
                        return true;
                    default:
                        throw new NotImplementedException();
                }
            }

            internal static void Bind(ConfigFile file)
            {
                void bindConfig(ConfigHolderBase config)
                {
                    config.Bind(file, SECTION_NAME, CONFIG_GUID, CONFIG_NAME);
                }

                bindConfig(HideActiveEffectsPanel);

                bindConfig(DisplayAlwaysActiveEffects);

                bindConfig(ActiveEffectsTextColor);

                bindConfig(DisplayNextEffect);

                bindConfig(NextEffectTimerDisplayMode);
            }
        }
    }
}
