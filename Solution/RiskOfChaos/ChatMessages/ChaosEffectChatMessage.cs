using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Formatting;
using RoR2;
using UnityEngine.Networking;

namespace RiskOfChaos.ChatMessages
{
    public class ChaosEffectChatMessage : ChatMessageBase
    {
        public string TokenFormat;
        public ChaosEffectIndex EffectIndex;
        public EffectNameFormatter EffectNameFormatter;
        public EffectNameFormatFlags EffectNameFormatFlags;

        public ChaosEffectChatMessage()
        {
        }

        public ChaosEffectChatMessage(string tokenFormat, ChaosEffectInfo effectInfo, EffectNameFormatFlags formatFlags)
        {
            TokenFormat = tokenFormat;
            EffectIndex = effectInfo.EffectIndex;
            EffectNameFormatter = effectInfo.LocalDisplayNameFormatter;
            EffectNameFormatFlags = formatFlags;
        }

        public override string ConstructChatString()
        {
            ChaosEffectInfo effectInfo = ChaosEffectCatalog.GetEffectInfo(EffectIndex);

            string effectDisplayName = "???";
            if (effectInfo != null)
            {
                effectDisplayName = effectInfo.GetDisplayName(EffectNameFormatter, EffectNameFormatFlags);
            }

            return Language.GetStringFormatted(TokenFormat, effectDisplayName);
        }

        public override void Serialize(NetworkWriter writer)
        {
            base.Serialize(writer);

            writer.Write(TokenFormat);
            writer.WritePackedIndex32((int)EffectIndex);
            writer.Write(EffectNameFormatter);
            writer.WritePackedUInt32((uint)EffectNameFormatFlags);
        }

        public override void Deserialize(NetworkReader reader)
        {
            base.Deserialize(reader);

            TokenFormat = reader.ReadString();
            EffectIndex = (ChaosEffectIndex)reader.ReadPackedIndex32();
            EffectNameFormatter = reader.ReadEffectNameFormatter();
            EffectNameFormatFlags = (EffectNameFormatFlags)reader.ReadPackedUInt32();
        }
    }
}
