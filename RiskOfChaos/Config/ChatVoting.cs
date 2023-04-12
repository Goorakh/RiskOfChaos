using BepInEx.Configuration;
using RiskOfChaos.EffectHandling.Controllers.ChatVoting;
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
                Twitch
            }

            static ConfigEntry<ChatVotingMode> _votingMode;
            const ChatVotingMode VOTING_MODE_DEFAULT_VALUE = ChatVotingMode.Disabled;

            public static ChatVotingMode VotingMode => _votingMode?.Value ?? VOTING_MODE_DEFAULT_VALUE;

            public static event Action OnVotingModeChanged;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool IsVotingDisabled()
            {
                return VotingMode == ChatVotingMode.Disabled;
            }

            static ConfigEntry<int> _numEffectOptionsConfig;
            const int NUM_EFFECT_OPTIONS_MIN_VALUE = 2;
            const int NUM_EFFECT_OPTIONS_DEFAULT_VALUE = 3;

            public static int NumEffectOptions
            {
                get
                {
                    if (_numEffectOptionsConfig == null)
                    {
                        return NUM_EFFECT_OPTIONS_DEFAULT_VALUE;
                    }
                    else
                    {
                        return Mathf.Max(_numEffectOptionsConfig.Value, NUM_EFFECT_OPTIONS_MIN_VALUE);
                    }
                }
            }

            public static event Action OnNumEffectOptionsChanged;

            static ConfigEntry<bool> _includeRandomEffectInVoteConfig;
            const bool INCLUDE_RANDOM_EFFECT_IN_VOTE_DEFAULT_VALUE = true;

            public static bool IncludeRandomEffectInVote => _includeRandomEffectInVoteConfig?.Value ?? INCLUDE_RANDOM_EFFECT_IN_VOTE_DEFAULT_VALUE;

            public static event Action OnIncludeRandomEffectInVoteChanged;

            static ConfigEntry<VoteWinnerSelectionMode> _voteWinnerSelectionModeConfig;
            const VoteWinnerSelectionMode VOTE_WINNER_SELECTION_MODE_DEFAULT_VALUE = VoteWinnerSelectionMode.MostVotes;

            public static VoteWinnerSelectionMode WinnerSelectionMode => _voteWinnerSelectionModeConfig?.Value ?? VOTE_WINNER_SELECTION_MODE_DEFAULT_VALUE;

            public static event Action OnWinnerSelectionModeChanged;

            internal static void Init(ConfigFile file)
            {
                const string SECTION_NAME = "Streamer Integration";

                _votingMode = file.Bind(new ConfigDefinition(SECTION_NAME, "Voting Mode"), VOTING_MODE_DEFAULT_VALUE);

                ModSettingsManager.AddOption(new ChoiceOption(_votingMode), CONFIG_GUID, CONFIG_NAME);

                _votingMode.SettingChanged += (s, e) =>
                {
                    OnVotingModeChanged?.Invoke();
                };

                _numEffectOptionsConfig = file.Bind(new ConfigDefinition(SECTION_NAME, "Num Effect Options"), NUM_EFFECT_OPTIONS_DEFAULT_VALUE, new ConfigDescription("The number of effects viewers can pick from during voting"));

                _numEffectOptionsConfig.SettingChanged += (s, e) =>
                {
                    OnNumEffectOptionsChanged?.Invoke();
                };

                ModSettingsManager.AddOption(new IntSliderOption(_numEffectOptionsConfig, new IntSliderConfig
                {
                    min = NUM_EFFECT_OPTIONS_MIN_VALUE,
                    max = 10,
                    checkIfDisabled = IsVotingDisabled
                }), CONFIG_GUID, CONFIG_NAME);

                _includeRandomEffectInVoteConfig = file.Bind(new ConfigDefinition(SECTION_NAME, "Include Random Effect In Vote"), INCLUDE_RANDOM_EFFECT_IN_VOTE_DEFAULT_VALUE, new ConfigDescription("If this is enabled, an additional option will be added to the effect vote list, which will activate a random effect instead of a specific one"));

                _includeRandomEffectInVoteConfig.SettingChanged += (s, e) =>
                {
                    OnIncludeRandomEffectInVoteChanged?.Invoke();
                };

                ModSettingsManager.AddOption(new CheckBoxOption(_includeRandomEffectInVoteConfig, new CheckBoxConfig
                {
                    checkIfDisabled = IsVotingDisabled
                }), CONFIG_GUID, CONFIG_NAME);

                _voteWinnerSelectionModeConfig = file.Bind(new ConfigDefinition(SECTION_NAME, "Vote Winner Selection Mode"), VOTE_WINNER_SELECTION_MODE_DEFAULT_VALUE, new ConfigDescription($"How the winner of any vote should be selected.\n\n{VoteWinnerSelectionMode.MostVotes} (Default): The vote with the most votes will be selected, if there is a tie, a random tied option is selected\n{VoteWinnerSelectionMode.RandomProportional}: Every option has a chance to be selected, weighted by the number of votes. Ex. an option with 70% of the votes will have a 70% chance to be selected."));

                _voteWinnerSelectionModeConfig.SettingChanged += (s, e) =>
                {
                    OnWinnerSelectionModeChanged?.Invoke();
                };

                ModSettingsManager.AddOption(new ChoiceOption(_voteWinnerSelectionModeConfig, new ChoiceConfig
                {
                    checkIfDisabled = IsVotingDisabled
                }), CONFIG_GUID, CONFIG_NAME);
            }
        }
    }
}
