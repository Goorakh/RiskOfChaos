using Newtonsoft.Json;
using System;

namespace RiskOfChaos.SaveHandling.DataContainers.EffectHandlerControllers
{
    [Serializable]
    public class EffectActivationSignalerChatVoteData
    {
        [JsonProperty("vm")]
        public Configs.ChatVoting.ChatVotingMode VotingMode;

        [JsonProperty("vsc")]
        public int VotesStartedCount;

        [JsonProperty("vs")]
        public SerializedEffectVoteInfo[] VoteSelection;
    }
}