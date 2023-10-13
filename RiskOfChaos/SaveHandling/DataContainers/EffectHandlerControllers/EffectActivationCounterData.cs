using System;
using System.Runtime.Serialization;

namespace RiskOfChaos.SaveHandling.DataContainers.EffectHandlerControllers
{
    [Serializable]
    public class EffectActivationCounterData
    {
        [DataMember(Name = "ac")]
        public SerializableEffectActivationCount[] ActivationCounts;
    }
}