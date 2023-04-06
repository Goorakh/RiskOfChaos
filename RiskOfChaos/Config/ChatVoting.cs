using BepInEx.Configuration;
using RiskOfOptions;
using RiskOfOptions.Options;

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

            internal static void Init(ConfigFile file)
            {
                const string SECTION_NAME = "ChatVoting";

                _votingMode = file.Bind(new ConfigDefinition(SECTION_NAME, "Voting Mode"), VOTING_MODE_DEFAULT_VALUE);

                ModSettingsManager.AddOption(new ChoiceOption(_votingMode), CONFIG_GUID, CONFIG_NAME);
            }
        }
    }
}
