using RiskOfChaos.Utilities;
using RoR2;
using UnityEngine.Networking;

namespace RiskOfChaos.Networking.SyncList
{
    public sealed class SyncListPickupPair : SyncListStruct<PickupPair>
    {
        public override void SerializeItem(NetworkWriter writer, PickupPair item)
        {
            writer.Write(item.PickupA);
            writer.Write(item.PickupB);
        }

        public override PickupPair DeserializeItem(NetworkReader reader)
        {
            PickupIndex pickupA = reader.ReadPickupIndex();
            PickupIndex pickupB = reader.ReadPickupIndex();

            return new PickupPair(pickupA, pickupB);
        }
    }
}
