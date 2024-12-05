using RoR2;
using UnityEngine.Networking;

namespace RiskOfChaos.Networking.SyncList
{
    public sealed class SyncListPickupIndex : SyncListStruct<PickupIndex>
    {
        public override void SerializeItem(NetworkWriter writer, PickupIndex item)
        {
            writer.Write(item);
        }

        public override PickupIndex DeserializeItem(NetworkReader reader)
        {
            return reader.ReadPickupIndex();
        }
    }
}
