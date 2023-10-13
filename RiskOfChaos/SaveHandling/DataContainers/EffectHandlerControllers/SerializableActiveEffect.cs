using System;
using System.Runtime.Serialization;

namespace RiskOfChaos.SaveHandling.DataContainers.EffectHandlerControllers
{
    [Serializable]
    public class SerializableActiveEffect
    {
        [DataMember(Name = "e")]
        public SerializableEffect Effect;

        [DataMember(Name = "sed")]
        public byte[] SerializedEffectData;
    }
}
