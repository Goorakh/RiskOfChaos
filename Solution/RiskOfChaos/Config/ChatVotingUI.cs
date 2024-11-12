using BepInEx.Configuration;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfOptions.OptionConfigs;
using UnityEngine;

namespace RiskOfChaos
{
    partial class Configs
    {
        public static class ChatVotingUI
        {
            public const string SECTION_NAME = "Streamer Integration: UI";

            public static readonly ConfigHolder<float> VoteDisplayScaleMultiplier =
                ConfigFactory<float>.CreateConfig("Vote Display UI Scale", 1f)
                                    .Description("The scale multiplier of the effect vote options display")
                                    .AcceptableValues(new AcceptableValueMin<float>(0f))
                                    .OptionConfig(new StepSliderConfig
                                    {
                                        FormatString = "{0:F2}X",
                                        min = 0f,
                                        max = 2.5f,
                                        increment = 0.05f
                                    })
                                    .MovedFrom(ChatVoting.SECTION_NAME)
                                    .Build();

            public static readonly ConfigHolder<Color> VoteDisplayTextColor =
                ConfigFactory<Color>.CreateConfig("Vote Display Text Color", new Color(1f, 1f, 1f, 1f))
                                    .Description("The color of the effect voting options text")
                                    .OptionConfig(new ColorOptionConfig())
                                    .MovedFrom(ChatVoting.SECTION_NAME)
                                    .Build();

            public static readonly ConfigHolder<Color> VoteDisplayBackgroundColor =
                ConfigFactory<Color>.CreateConfig("Vote Display Background Color", new Color(0.0943f, 0.0943f, 0.0943f, 0.3373f))
                                    .Description("The color of the effect voting options backdrop")
                                    .OptionConfig(new ColorOptionConfig())
                                    .MovedFrom(ChatVoting.SECTION_NAME)
                                    .Build();

            public enum VoteDisplayScalingMode
            {
                Disabled,
                Smooth,
                Immediate
            }

            public static readonly ConfigHolder<VoteDisplayScalingMode> VoteDisplayScalingModeConfig =
                ConfigFactory<VoteDisplayScalingMode>.CreateConfig("Vote Display Text Scaling Mode", VoteDisplayScalingMode.Smooth)
                                                     .Description($"""
                                                      Controls how the vote options text will be scaled depending on how many votes that option has

                                                      {nameof(VoteDisplayScalingMode.Disabled)}: No scaling is done, all options are always displayed exactly the same

                                                      {nameof(VoteDisplayScalingMode.Smooth)}: Scaling is done, and interpolated to smoothly approach the target scale

                                                      {nameof(VoteDisplayScalingMode.Immediate)}: Scaling is done, and applied immediately instead of smoothly interpolating
                                                      """)
                                                     .OptionConfig(new ChoiceConfig())
                                                     .Build();

            internal static void Bind(ConfigFile file)
            {
                void bindConfig(ConfigHolderBase configHolder)
                {
                    configHolder.Bind(file, SECTION_NAME, CONFIG_GUID, CONFIG_NAME);
                }

                bindConfig(VoteDisplayScaleMultiplier);

                bindConfig(VoteDisplayTextColor);

                bindConfig(VoteDisplayBackgroundColor);

                bindConfig(VoteDisplayScalingModeConfig);
            }
        }
    }
}
