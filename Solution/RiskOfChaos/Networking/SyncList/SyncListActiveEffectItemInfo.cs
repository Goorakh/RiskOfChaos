using RiskOfChaos.UI.ActiveEffectsPanel;
using UnityEngine.Networking;

namespace RiskOfChaos.Networking.SyncLists
{
    public class SyncListActiveEffectItemInfo : SyncListStruct<ActiveEffectItemInfo>
    {
        public override void SerializeItem(NetworkWriter writer, ActiveEffectItemInfo item)
        {
            item.Serialize(writer);
        }

        public override ActiveEffectItemInfo DeserializeItem(NetworkReader reader)
        {
            return ActiveEffectItemInfo.Deserialize(reader);
        }
    }
}
