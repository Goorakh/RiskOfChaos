using RiskOfChaos.EffectHandling.Controllers;
using System;
using System.Runtime.Serialization;

namespace RiskOfChaos.SaveHandling.DataContainers.EffectHandlerControllers
{
    [Serializable]
    public class SerializableActiveEffect
    {
        [DataMember(Name = "e")]
        public SerializableEffect Effect;

        [DataMember(Name = "da")]
        public ChaosEffectDispatchArgs DispatchArgs;

        [DataMember(Name = "sed")]
        public string SerializedEffectDataBase64;

        [IgnoreDataMember]
        public byte[] SerializedEffectDataBytes
        {
            get
            {
                return Convert.FromBase64String(SerializedEffectDataBase64);
            }
            set
            {
                SerializedEffectDataBase64 = Convert.ToBase64String(value);
            }
        }
    }
}
