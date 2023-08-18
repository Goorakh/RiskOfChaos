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
    }
}
