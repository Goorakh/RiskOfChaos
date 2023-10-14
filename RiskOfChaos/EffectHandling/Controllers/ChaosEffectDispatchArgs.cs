using System;
using System.Runtime.Serialization;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling.Controllers
{
    [Serializable]
    public struct ChaosEffectDispatchArgs
    {
        [DataMember(Name = "f")]
        public EffectDispatchFlags DispatchFlags = EffectDispatchFlags.None;

        [IgnoreDataMember]
        public ulong? OverrideRNGSeed;

        public ChaosEffectDispatchArgs()
        {
        }

        public ChaosEffectDispatchArgs(NetworkReader reader)
        {
            DispatchFlags = (EffectDispatchFlags)reader.ReadPackedUInt32();
        }

        public readonly void Serialize(NetworkWriter writer)
        {
            writer.WritePackedUInt32((uint)DispatchFlags);
        }

        public readonly bool HasFlag(EffectDispatchFlags flag)
        {
            return (DispatchFlags & flag) != 0;
        }
    }
}
