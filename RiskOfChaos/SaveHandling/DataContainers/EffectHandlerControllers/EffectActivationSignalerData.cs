using System;
using System.Runtime.Serialization;

namespace RiskOfChaos.SaveHandling.DataContainers.EffectHandlerControllers
{
    [Serializable]
    public class EffectActivationSignalerData
    {
        [DataMember(Name = "rng")]
        public SerializableRng NextEffectRng;
    }
}