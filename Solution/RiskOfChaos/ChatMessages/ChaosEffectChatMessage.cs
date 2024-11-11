using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Formatting;
using RiskOfChaos.Utilities.Extensions;
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

        public ChaosEffectChatMessage(string tokenFormat, ChaosEffectIndex effectIndex, EffectNameFormatter nameFormatter, EffectNameFormatFlags formatFlags)
        {
            TokenFormat = tokenFormat;
            EffectIndex = effectIndex;
            EffectNameFormatter = nameFormatter;
            EffectNameFormatFlags = formatFlags;
        }

        public ChaosEffectChatMessage(string tokenFormat, ChaosEffectIndex effectIndex, EffectNameFormatFlags formatFlags) : this(tokenFormat, effectIndex, ChaosEffectCatalog.GetEffectStaticNameFormatter(effectIndex), formatFlags)
        {
        }

        public override string ConstructChatString()
        {
            ChaosEffectInfo effectInfo = ChaosEffectCatalog.GetEffectInfo(EffectIndex);

            return Language.GetStringFormatted(TokenFormat, EffectNameFormatter.GetEffectDisplayName(effectInfo, EffectNameFormatFlags));
        }

        public override void Serialize(NetworkWriter writer)
        {
            base.Serialize(writer);

            writer.Write(TokenFormat);
            writer.WriteChaosEffectIndex(EffectIndex);
            writer.Write(EffectNameFormatter);
            writer.WritePackedUInt32((uint)EffectNameFormatFlags);
        }

        public override void Deserialize(NetworkReader reader)
        {
            base.Deserialize(reader);

            TokenFormat = reader.ReadString();
            EffectIndex = reader.ReadChaosEffectIndex();
            EffectNameFormatter = reader.ReadEffectNameFormatter();
            EffectNameFormatFlags = (EffectNameFormatFlags)reader.ReadPackedUInt32();
        }
    }
}
