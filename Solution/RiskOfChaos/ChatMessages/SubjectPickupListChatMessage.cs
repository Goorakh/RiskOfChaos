using RoR2;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.ChatMessages
{
    public sealed class SubjectPickupListChatMessage : SubjectChatMessage
    {
        static readonly StringBuilder _sharedStringBuilder = new StringBuilder();

        public PickupIndex[] PickupIndices;

        public uint[] PickupQuantities;

        public override string ConstructChatString()
        {
            string subjectName = GetSubjectName();

            _sharedStringBuilder.Clear();

            int pickupsCount = PickupIndices.Length;
            for (int i = 0; i < pickupsCount; i++)
            {
                PickupIndex pickupIndex = PickupIndices[i];
                uint pickupQuantity = PickupQuantities[i];

                PickupDef pickupDef = PickupCatalog.GetPickupDef(pickupIndex);

                string pickupNameToken = PickupCatalog.invalidPickupToken;
                Color pickupColor = PickupCatalog.invalidPickupColor;

                if (pickupDef != null)
                {
                    pickupNameToken = pickupDef.nameToken;
                    pickupColor = pickupDef.baseColor;
                }

                _sharedStringBuilder.Append("<color=#")
                                    .AppendColor32RGBHexValues(pickupColor)
                                    .Append('>')
                                    .Append(Language.GetString(pickupNameToken))
                                    .Append("</color>");

                if (pickupQuantity != 1)
                {
                    _sharedStringBuilder.Append('(')
                                        .Append(pickupQuantity)
                                        .Append(')');
                }

                if (i != pickupsCount - 1)
                {
                    _sharedStringBuilder.Append(", ");
                }
            }

            return Language.GetStringFormatted(GetResolvedToken(), subjectName, _sharedStringBuilder);
        }

        public override void Serialize(NetworkWriter writer)
        {
            base.Serialize(writer);

            writer.WritePackedUInt32((uint)PickupIndices.Length);

            for (int i = 0; i < PickupIndices.Length; i++)
            {
                writer.Write(PickupIndices[i]);
                writer.WritePackedUInt32(PickupQuantities[i]);
            }
        }

        public override void Deserialize(NetworkReader reader)
        {
            base.Deserialize(reader);

            uint pickupsCount = reader.ReadPackedUInt32();

            PickupIndices = new PickupIndex[pickupsCount];
            PickupQuantities = new uint[pickupsCount];

            for (int i = 0; i < pickupsCount; i++)
            {
                PickupIndices[i] = reader.ReadPickupIndex();
                PickupQuantities[i] = reader.ReadPackedUInt32();
            }
        }
    }
}
