using RiskOfChaos.EffectHandling;
using System;
using System.Runtime.Serialization;

namespace RiskOfChaos.SaveHandling.DataContainers.EffectHandlerControllers
{
    [Serializable]
    public class SerializableEffect
    {
        [DataMember(Name = "i")]
        public string Identifier;

        [IgnoreDataMember]
        public ChaosEffectIndex EffectIndex => ChaosEffectCatalog.FindEffectIndex(Identifier);

        [IgnoreDataMember]
        public ChaosEffectInfo EffectInfo => ChaosEffectCatalog.GetEffectInfo(EffectIndex);

        public SerializableEffect()
        {
        }

        public SerializableEffect(string identifier)
        {
            Identifier = identifier;
        }

        public SerializableEffect(ChaosEffectInfo effectInfo) : this(effectInfo?.Identifier ?? string.Empty)
        {
        }

        public SerializableEffect(ChaosEffectIndex effectIndex) : this(ChaosEffectCatalog.GetEffectInfo(effectIndex))
        {
        }

        public override string ToString()
        {
            return Identifier;
        }
    }
}