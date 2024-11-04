using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.ChatMessages
{
    public sealed class PickupNameFormatMessage : ChatMessageBase
    {
        public string Token;
        public PickupIndex[] PickupIndices;

        public override string ConstructChatString()
        {
            string[] pickupNames = new string[PickupIndices.Length];

            for (int i = 0; i < PickupIndices.Length; i++)
            {
                PickupDef pickupDef = PickupCatalog.GetPickupDef(PickupIndices[i]);

                string pickupNameToken = PickupCatalog.invalidPickupToken;
                Color pickupColor = PickupCatalog.invalidPickupColor;
                if (pickupDef != null)
                {
                    pickupNameToken = pickupDef.nameToken;
                    pickupColor = pickupDef.baseColor;
                }

                pickupNames[i] = Util.GenerateColoredString(Language.GetString(pickupNameToken), pickupColor);
            }

            return Language.GetStringFormatted(Token, pickupNames);
        }

        public override void Serialize(NetworkWriter writer)
        {
            base.Serialize(writer);

            writer.Write(Token);

            writer.WritePackedUInt32((uint)PickupIndices.Length);
            foreach (PickupIndex pickupIndex in PickupIndices)
            {
                writer.Write(pickupIndex);
            }
        }

        public override void Deserialize(NetworkReader reader)
        {
            base.Deserialize(reader);

            Token = reader.ReadString();

            uint pickupsCount = reader.ReadPackedUInt32();
            PickupIndices = new PickupIndex[pickupsCount];

            for (int i = 0; i < pickupsCount; i++)
            {
                PickupIndices[i] = reader.ReadPickupIndex();
            }
        }
    }
}
