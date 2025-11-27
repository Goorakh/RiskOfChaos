using RoR2;
using UnityEngine.Networking;

namespace RiskOfChaos.Networking.SyncList
{
    public sealed class SyncListUniquePickup : SyncListStruct<UniquePickup>
    {
        public override void SerializeItem(NetworkWriter writer, UniquePickup item)
        {
            writer.Write(item);
        }

        public override UniquePickup DeserializeItem(NetworkReader reader)
        {
            return reader.ReadUniquePickup();
        }
    }
}
