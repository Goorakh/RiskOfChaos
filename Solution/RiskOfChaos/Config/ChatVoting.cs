﻿using BepInEx.Configuration;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.EffectHandling.Controllers.ChatVoting;
using RiskOfChaos.Twitch;
using RiskOfOptions;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using System;
using System.Runtime.CompilerServices;

namespace RiskOfChaos
{
    partial class Configs
    {
        public static class ChatVoting
        {
            public const string SECTION_NAME = "Streamer Integration";

            public enum ChatVotingMode
            {
                Disabled,
                Twitch
            }

            public static readonly ConfigHolder<ChatVotingMode> VotingMode =
                ConfigFactory<ChatVotingMode>.CreateConfig("Voting Mode", ChatVotingMode.Disabled)
                                             .OptionConfig(new ChoiceConfig())
                                             .Build();

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static bool isVotingDisabled()
            {
                return VotingMode.Value == ChatVotingMode.Disabled;
            }

            public static event Action OnReconnectButtonPressed;

            public static readonly ConfigHolder<string> OverrideChannelName =
                ConfigFactory<string>.CreateConfig("Override Channel Name", string.Empty)
                                     .Description("Used to specify a different channel the mod will connect to, leave empty to use the channel of the account that you authenticated with")
                                     .OptionConfig(new InputFieldConfig
                                     {
                                        lineType = TMPro.TMP_InputField.LineType.SingleLine,
                                        richText = false,
                                        submitOn = InputFieldConfig.SubmitEnum.OnExitOrSubmit,
                                        checkIfDisabled = isVotingDisabled
                                     })
                                     .Build();

            const int NUM_EFFECT_OPTIONS_MIN_VALUE = 2;

            public static readonly ConfigHolder<int> NumEffectOptions =
                ConfigFactory<int>.CreateConfig("Num Effect Options", 3)
                                  .Description("The number of effects viewers can pick from during voting")
                                  .AcceptableValues(new AcceptableValueMin<int>(NUM_EFFECT_OPTIONS_MIN_VALUE))
                                  .OptionConfig(new IntFieldConfig
                                  {
                                      Min = NUM_EFFECT_OPTIONS_MIN_VALUE,
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
                                                      .Description($"""
                                                       How the winner of any vote should be selected.
                                                       
                                                       {nameof(VoteWinnerSelectionMode.MostVotes)} (Default): The vote with the most votes will be selected, if there is a tie, a random tied option is selected
                                                       {nameof(VoteWinnerSelectionMode.RandomProportional)}: Every option has a chance to be selected, weighted by the number of votes. Ex. an option with 70% of the votes will have a 70% chance to be selected.
                                                       """)
                                                      .OptionConfig(new ChoiceConfig
                                                      {
                                                          checkIfDisabled = isVotingDisabled
                                                      })
                                                      .Build();

            internal static void Bind(ConfigFile file)
            {
                void bindConfig(ConfigHolderBase configHolder)
                {
                    configHolder.Bind(file, SECTION_NAME, CONFIG_GUID, CONFIG_NAME);
                }

                bindConfig(VotingMode);

                ModSettingsManager.AddOption(new GenericButtonOption("Authenticate (Twitch)", SECTION_NAME, "Authenticate your account with Risk of Chaos (Opens browser tab)", "Open", TwitchAuthenticationManager.AuthenticateNewToken), CONFIG_GUID, CONFIG_NAME);

                bindConfig(OverrideChannelName);

                ModSettingsManager.AddOption(new GenericButtonOption("Manual reconnect", SECTION_NAME, "Use this to manually reconnect the mod to your channel if connection is lost", "Reconnect", () =>
                {
                    OnReconnectButtonPressed?.Invoke();
                }), CONFIG_GUID, CONFIG_NAME);

                bindConfig(NumEffectOptions);

                bindConfig(IncludeRandomEffectInVote);

                bindConfig(WinnerSelectionMode);
            }
        }
    }
}
