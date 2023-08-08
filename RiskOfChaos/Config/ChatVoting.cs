using BepInEx.Configuration;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling.Controllers.ChatVoting;
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
            public enum ChatVotingMode
            {
                Disabled,
                Twitch
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

            public static event Action OnReconnectButtonPressed;

            const int NUM_EFFECT_OPTIONS_MIN_VALUE = 2;

            public static readonly ConfigHolder<int> NumEffectOptions =
                ConfigFactory<int>.CreateConfig("Num Effect Options", 3)
                                  .Description("The number of effects viewers can pick from during voting")
                                  .OptionConfig(new IntSliderConfig
                                  {
                                      min = NUM_EFFECT_OPTIONS_MIN_VALUE,
                                      max = 10,
                                      checkIfDisabled = isVotingDisabled
                                  })
                                  .ValueConstrictor(CommonValueConstrictors.GreaterThanOrEqualTo(NUM_EFFECT_OPTIONS_MIN_VALUE))
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
                                                      .Description($"How the winner of any vote should be selected.\n\n{VoteWinnerSelectionMode.MostVotes} (Default): The vote with the most votes will be selected, if there is a tie, a random tied option is selected\n{VoteWinnerSelectionMode.RandomProportional}: Every option has a chance to be selected, weighted by the number of votes. Ex. an option with 70% of the votes will have a 70% chance to be selected.")
                                                      .OptionConfig(new ChoiceConfig
                                                      {
                                                          checkIfDisabled = isVotingDisabled
                                                      })
                                                      .ValueValidator(CommonValueValidators.DefinedEnumValue<VoteWinnerSelectionMode>())
                                                      .Build();

            public static readonly ConfigHolder<float> VoteDisplayScaleMultiplier =
                ConfigFactory<float>.CreateConfig("Vote Display UI Scale", 1f)
                                    .Description("The scale multiplier of the vote options display")
                                    .OptionConfig(new StepSliderConfig
                                    {
                                        formatString = "{0:F2}X",
                                        min = 0f,
                                        max = 2.5f,
                                        increment = 0.05f,
                                        checkIfDisabled = isVotingDisabled
                                    })
                                    .ValueConstrictor(CommonValueConstrictors.GreaterThanOrEqualTo(0f))
                                    .Build();

            internal static void Bind(ConfigFile file)
            {
                const string SECTION_NAME = "Streamer Integration";

                void bindConfig<T>(ConfigHolder<T> configHolder)
                {
                    configHolder.Bind(file, SECTION_NAME, CONFIG_GUID, CONFIG_NAME);
                }

                bindConfig(VotingMode);

                ModSettingsManager.AddOption(new GenericButtonOption("Manual reconnect", SECTION_NAME, "Use this to manually reconnect the mod to your channel if connection is lost", "Reconnect", () =>
                {
                    OnReconnectButtonPressed?.Invoke();
                }), CONFIG_GUID, CONFIG_NAME);

                bindConfig(NumEffectOptions);

                bindConfig(IncludeRandomEffectInVote);

                bindConfig(WinnerSelectionMode);

                bindConfig(VoteDisplayScaleMultiplier);
            }
        }
    }
}
