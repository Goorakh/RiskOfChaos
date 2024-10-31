using Newtonsoft.Json;
using System;

namespace RiskOfChaos.SaveHandling.DataContainers.EffectHandlerControllers
{
    [Serializable]
    public class EffectActivationSignalerChatVoteData
    {
        [JsonProperty("m")]
        public Configs.ChatVoting.ChatVotingMode VotingMode;

        [JsonProperty("c")]
        public int VotesStartedCount;

        [JsonProperty("s")]
        public SerializedEffectVoteInfo[] VoteSelection;
    }
}