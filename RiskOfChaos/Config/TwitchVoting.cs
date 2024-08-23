using BepInEx.Configuration;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.Twitch;
using RiskOfOptions;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RiskOfTwitch.Chat.Poll;
using System.Runtime.CompilerServices;

namespace RiskOfChaos
{
    partial class Configs
    {
        public static class TwitchVoting
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static bool isNotUsingTwitchPollVoting()
            {
                return ChatVoting.VotingMode.Value != ChatVoting.ChatVotingMode.TwitchPolls;
            }

            public static readonly ConfigHolder<bool> AllowChannelPointPollVotes =
                ConfigFactory<bool>.CreateConfig("Allow Channel Point Voting for Polls", false)
                                   .Description("If viewers should be able to cast additional votes using channel points when voting for the next effect via Twitch Polls. (1 vote is always free for all viewers)\nMake sure Channel Points are enabled in your channel settings, otherwise polls will not appear at all.")
                                   .OptionConfig(new CheckBoxConfig
                                   {
                                       checkIfDisabled = isNotUsingTwitchPollVoting
                                   })
                                   .Build();

            public static readonly ConfigHolder<int> ChannelPointsPerAdditionalVote =
                ConfigFactory<int>.CreateConfig("Channel Point Vote Cost", 200)
                                  .Description("How many channel points a viewer must spend to cast additional votes")
                                  .AcceptableValues(new AcceptableValueRange<int>(CreatePollArgs.MIN_CHANNEL_POINTS_PER_VOTE, CreatePollArgs.MAX_CHANNEL_POINTS_PER_VOTE))
                                  .OptionConfig(new IntFieldConfig
                                  {
                                      Min = CreatePollArgs.MIN_CHANNEL_POINTS_PER_VOTE,
                                      Max = CreatePollArgs.MAX_CHANNEL_POINTS_PER_VOTE,
                                      checkIfDisabled = () => isNotUsingTwitchPollVoting() || !AllowChannelPointPollVotes.Value
                                  })
                                  .Build();

            internal static void Bind(ConfigFile file)
            {
                const string SECTION_NAME = "Streamer Integration (Twitch)";

                void bindConfig(ConfigHolderBase configHolder)
                {
                    configHolder.Bind(file, SECTION_NAME, CONFIG_GUID, CONFIG_NAME);
                }

                ModSettingsManager.AddOption(new GenericButtonOption("Authenticate", SECTION_NAME, "Authenticate your Twitch account with Risk of Chaos (Opens browser tab)", "Open", TwitchAuthenticationManager.AuthenticateNewToken), CONFIG_GUID, CONFIG_NAME);

                bindConfig(AllowChannelPointPollVotes);

                bindConfig(ChannelPointsPerAdditionalVote);
            }
        }
    }
}
