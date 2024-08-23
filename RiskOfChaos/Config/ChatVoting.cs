using BepInEx.Configuration;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.EffectHandling.Controllers.ChatVoting;
using RiskOfChaos.Twitch;
using RiskOfOptions;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace RiskOfChaos
{
    partial class Configs
    {
        public static class ChatVoting
        {
            public enum ChatVotingMode
            {
                Disabled,
                Twitch,
                TwitchPolls
            }

            public static readonly ConfigHolder<ChatVotingMode> VotingMode =
                ConfigFactory<ChatVotingMode>.CreateConfig("Voting Mode", ChatVotingMode.Disabled)
                                             .OptionConfig(new ChoiceConfig())
                                             .ValueValidator(CommonValueValidators.DefinedEnumValue<ChatVotingMode>())
                                             .Build();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static bool isVotingDisabled()
            {
                return VotingMode.Value == ChatVotingMode.Disabled;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static bool isVotingDisabledOrUsingThirdPartyPolls()
            {
                switch (VotingMode.Value)
                {
                    case ChatVotingMode.Disabled:
                    case ChatVotingMode.TwitchPolls:
                        return true;
                    default:
                        return false;
                }
            }

            public static event Action OnReconnectButtonPressed;

            public static readonly ConfigHolder<string> OverrideChannelName =
                ConfigFactory<string>.CreateConfig("Override Channel Name", string.Empty)
                                     .Description("Used to specify a different channel the mod will connect to, leave empty to use the channel of the account that you authenticated with, does not work in any external poll voting mode")
                                     .OptionConfig(new InputFieldConfig
                                     {
                                        lineType = TMPro.TMP_InputField.LineType.SingleLine,
                                        richText = false,
                                        submitOn = InputFieldConfig.SubmitEnum.OnExitOrSubmit,
                                        checkIfDisabled = isVotingDisabledOrUsingThirdPartyPolls
                                     })
                                     .Build();

            const int NUM_EFFECT_OPTIONS_MIN_VALUE = 2;

            public static readonly ConfigHolder<int> NumEffectOptions =
                ConfigFactory<int>.CreateConfig("Num Effect Options", 3)
                                  .Description("The number of effects viewers can pick from during voting")
                                  .AcceptableValues(new AcceptableValueMin<int>(NUM_EFFECT_OPTIONS_MIN_VALUE))
                                  .OptionConfig(new IntSliderConfig
                                  {
                                      min = NUM_EFFECT_OPTIONS_MIN_VALUE,
                                      max = 10,
                                      checkIfDisabled = isVotingDisabled
                                  })
                                  .Build();

            public static readonly ConfigHolder<bool> IncludeRandomEffectInVote =
                ConfigFactory<bool>.CreateConfig("Include Random Effect In Vote", true)
                                   .Description("If this is enabled, an additional option will be added to the effect vote list, which will activate a random effect instead of a specific one")
                                   .OptionConfig(new CheckBoxConfig
                                   {
                                       checkIfDisabled = isVotingDisabled
                                   })
                                   .Build();

            public static readonly ConfigHolder<VoteWinnerSelectionMode> WinnerSelectionMode =
                ConfigFactory<VoteWinnerSelectionMode>.CreateConfig("Vote Winner Selection Mode", VoteWinnerSelectionMode.MostVotes)
                                                      .Description($"How the winner of any vote should be selected.\n\n{nameof(VoteWinnerSelectionMode.MostVotes)} (Default): The vote with the most votes will be selected, if there is a tie, a random tied option is selected\n{nameof(VoteWinnerSelectionMode.RandomProportional)}: Every option has a chance to be selected, weighted by the number of votes. Ex. an option with 70% of the votes will have a 70% chance to be selected.\n\nAny external poll voting mode will always use {nameof(VoteWinnerSelectionMode.MostVotes)}")
                                                      .OptionConfig(new ChoiceConfig
                                                      {
                                                          checkIfDisabled = isVotingDisabledOrUsingThirdPartyPolls
                                                      })
                                                      .ValueValidator(CommonValueValidators.DefinedEnumValue<VoteWinnerSelectionMode>())
                                                      .Build();

            public static readonly ConfigHolder<float> VoteDisplayScaleMultiplier =
                ConfigFactory<float>.CreateConfig("Vote Display UI Scale", 1f)
                                    .Description("The scale multiplier of the vote options display")
                                    .AcceptableValues(new AcceptableValueMin<float>(0f))
                                    .OptionConfig(new StepSliderConfig
                                    {
                                        formatString = "{0:F2}X",
                                        min = 0f,
                                        max = 2.5f,
                                        increment = 0.05f,
                                        checkIfDisabled = isVotingDisabledOrUsingThirdPartyPolls
                                    })
                                    .Build();

            public static readonly ConfigHolder<Color> VoteDisplayTextColor =
                ConfigFactory<Color>.CreateConfig("Vote Display Text Color", new Color(1f, 1f, 1f, 1f))
                                    .Description("The color of the effect voting options text")
                                    .OptionConfig(new ColorOptionConfig
                                    {
                                        checkIfDisabled = isVotingDisabledOrUsingThirdPartyPolls
                                    })
                                    .Build();

            public static readonly ConfigHolder<Color> VoteDisplayBackgroundColor =
                ConfigFactory<Color>.CreateConfig("Vote Display Background Color", new Color(0.0943f, 0.0943f, 0.0943f, 0.3373f))
                                    .Description("The color of the effect voting options backdrop")
                                    .OptionConfig(new ColorOptionConfig
                                    {
                                        checkIfDisabled = isVotingDisabledOrUsingThirdPartyPolls
                                    })
                                    .Build();

            internal static void Bind(ConfigFile file)
            {
                const string SECTION_NAME = "Streamer Integration";

                void bindConfig<T>(ConfigHolder<T> configHolder)
                {
                    configHolder.Bind(file, SECTION_NAME, CONFIG_GUID, CONFIG_NAME);
                }

                bindConfig(VotingMode);

                bindConfig(OverrideChannelName);

                ModSettingsManager.AddOption(new GenericButtonOption("Manual reconnect", SECTION_NAME, "Use this to manually reconnect the mod to your channel if connection is lost", "Reconnect", () =>
                {
                    OnReconnectButtonPressed?.Invoke();
                }), CONFIG_GUID, CONFIG_NAME);

                bindConfig(NumEffectOptions);

                bindConfig(IncludeRandomEffectInVote);

                bindConfig(WinnerSelectionMode);

                bindConfig(VoteDisplayScaleMultiplier);

                bindConfig(VoteDisplayTextColor);

                bindConfig(VoteDisplayBackgroundColor);
            }
        }
    }
}
