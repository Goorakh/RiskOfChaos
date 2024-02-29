using System;
using System.Runtime.Serialization;

namespace RiskOfChaos.SaveHandling.DataContainers.EffectHandlerControllers
{
    [Serializable]
    public class SerializedEffectVoteInfo
    {
        [DataMember(Name = "uv")]
        public string[] UserVotes;

        [DataMember(Name = "e")]
        public SerializableEffect Effect;

        [DataMember(Name = "r")]
        public bool IsRandom;
    }
}
