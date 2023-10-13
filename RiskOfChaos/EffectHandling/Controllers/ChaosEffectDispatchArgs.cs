using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling.Controllers
{
    public struct ChaosEffectDispatchArgs
    {
        public EffectDispatchFlags DispatchFlags = EffectDispatchFlags.None;

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
