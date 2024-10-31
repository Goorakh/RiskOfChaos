using Newtonsoft.Json;
using RiskOfChaos.EffectHandling;
using System;

namespace RiskOfChaos.SaveHandling.DataContainers.EffectHandlerControllers
{
    [Serializable]
    public class SerializedEffectVoteInfo
    {
        [JsonProperty("v")]
        public string[] UserVotes;

        [JsonProperty("e")]
        public ChaosEffectIndex Effect;

        [JsonProperty("r")]
        public bool IsRandom;
    }
}
