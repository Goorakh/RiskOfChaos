using RiskOfChaos.EffectHandling;
using RoR2;
using UnityEngine.Networking;

namespace RiskOfChaos.Utilities.Extensions
{
    public static class NetworkExtensions
    {
        public static void WriteChaosEffectIndex(this NetworkWriter writer, ChaosEffectIndex effectIndex)
        {
            writer.WritePackedIndex32((int)effectIndex);
        }

        public static ChaosEffectIndex ReadChaosEffectIndex(this NetworkReader reader)
        {
            return (ChaosEffectIndex)reader.ReadPackedIndex32();
        }

        public static void WriteRNG(this NetworkWriter writer, Xoroshiro128Plus rng)
        {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
            writer.WritePackedUInt64(rng.state0);
            writer.WritePackedUInt64(rng.state1);
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
        }

        public static Xoroshiro128Plus ReadRNG(this NetworkReader reader)
        {
            return new Xoroshiro128Plus(0)
            {
                state0 = reader.ReadPackedUInt64(),
                state1 = reader.ReadPackedUInt64()
            };
        }
    }
}
