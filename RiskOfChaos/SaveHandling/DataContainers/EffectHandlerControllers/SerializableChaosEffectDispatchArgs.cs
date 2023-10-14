using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using System;
using System.Runtime.Serialization;

namespace RiskOfChaos.SaveHandling.DataContainers.EffectHandlerControllers
{
    [Serializable]
    public class SerializableChaosEffectDispatchArgs
    {
        [DataMember(Name = "f")]
        public EffectDispatchFlags DispatchFlags;

        [DataMember(Name = "os")]
        public ulong? OverrideRNGSeed;

        public static implicit operator ChaosEffectDispatchArgs(SerializableChaosEffectDispatchArgs serializableArgs)
        {
            return new ChaosEffectDispatchArgs
            {
                DispatchFlags = serializableArgs.DispatchFlags,
                OverrideRNGSeed = serializableArgs.OverrideRNGSeed
            };
        }

        public static implicit operator SerializableChaosEffectDispatchArgs(ChaosEffectDispatchArgs args)
        {
            return new SerializableChaosEffectDispatchArgs
            {
                DispatchFlags = args.DispatchFlags,
                OverrideRNGSeed = args.OverrideRNGSeed
            };
        }
    }
}
