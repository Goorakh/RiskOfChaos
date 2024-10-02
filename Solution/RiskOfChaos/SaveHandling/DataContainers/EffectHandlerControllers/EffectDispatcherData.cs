using System;
using System.Runtime.Serialization;

namespace RiskOfChaos.SaveHandling.DataContainers.EffectHandlerControllers
{
    [Serializable]
    public class EffectDispatcherData
    {
        [DataMember(Name = "rng")]
        public SerializableRng EffectRNG;

        [DataMember(Name = "edc")]
        public ulong EffectDispatchCount;
    }
}