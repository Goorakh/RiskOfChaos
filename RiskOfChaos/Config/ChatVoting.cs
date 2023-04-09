using BepInEx.Configuration;
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
                None,
                Twitch
            }

            static ConfigEntry<ChatVotingMode> _votingMode;
            const ChatVotingMode VOTING_MODE_DEFAULT_VALUE = ChatVotingMode.None;

            public static ChatVotingMode VotingMode
            {
                get
                {
                    if (_votingMode == null)
                    {
                        return VOTING_MODE_DEFAULT_VALUE;
                    }
                    else
                    {
                        return _votingMode.Value;
                    }
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool IsVotingDisabled()
            {
                return VotingMode == ChatVotingMode.None;
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

            public static bool IncludeRandomEffectInVote
            {
                get
                {
                    return _includeRandomEffectInVoteConfig?.Value ?? INCLUDE_RANDOM_EFFECT_IN_VOTE_DEFAULT_VALUE;
                }
            }

            public static event Action OnIncludeRandomEffectInVoteChanged;

            internal static void Init(ConfigFile file)
            {
                const string SECTION_NAME = "Streamer Integration";

                _votingMode = file.Bind(new ConfigDefinition(SECTION_NAME, "Voting Mode"), VOTING_MODE_DEFAULT_VALUE);

                ModSettingsManager.AddOption(new ChoiceOption(_votingMode), CONFIG_GUID, CONFIG_NAME);

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
            }
        }
    }
}
