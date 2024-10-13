using UnityEngine.Networking;

namespace RiskOfChaos.Components.MaterialInterpolation
{
    public class SyncListMaterialPropertyInterpolationData : SyncListStruct<MaterialPropertyInterpolationData>
    {
        public override void SerializeItem(NetworkWriter writer, MaterialPropertyInterpolationData item)
        {
            item.Serialize(writer);
        }

        public override MaterialPropertyInterpolationData DeserializeItem(NetworkReader reader)
        {
            return MaterialPropertyInterpolationData.Deserialize(reader);
        }
    }
}
