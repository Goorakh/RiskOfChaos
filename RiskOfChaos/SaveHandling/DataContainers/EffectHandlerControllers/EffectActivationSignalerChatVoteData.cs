using System;
using System.Runtime.Serialization;

namespace RiskOfChaos.SaveHandling.DataContainers.EffectHandlerControllers
{
    [Serializable]
    public class EffectActivationSignalerChatVoteData
    {
        [DataMember(Name = "vm")]
        public Configs.ChatVoting.ChatVotingMode VotingMode;

        [DataMember(Name = "vsc")]
        public int VotesStartedCount;

        [DataMember(Name = "ovn")]
        public bool OffsetVoteNumbers;

        [DataMember(Name = "vs")]
        public SerializedEffectVoteInfo[] VoteSelection;
    }
}