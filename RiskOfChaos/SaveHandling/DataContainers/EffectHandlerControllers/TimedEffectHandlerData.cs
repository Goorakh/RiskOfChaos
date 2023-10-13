using System;
using System.Runtime.Serialization;

namespace RiskOfChaos.SaveHandling.DataContainers.EffectHandlerControllers
{
    [Serializable]
    public class TimedEffectHandlerData
    {
        [DataMember(Name = "ate")]
        public SerializableActiveEffect[] ActiveTimedEffects;
    }
}
