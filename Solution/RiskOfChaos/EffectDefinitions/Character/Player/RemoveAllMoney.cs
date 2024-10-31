using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character.Player
{
    [ChaosEffect("remove_all_money", DefaultSelectionWeight = 0.6f)]
    public sealed class RemoveAllMoney : MonoBehaviour
    {
        void Start()
        {
            if (NetworkServer.active)
            {
                PlayerUtils.GetAllPlayerMasters(false).TryDo(master =>
                {
                    master.money = 0;
                }, Util.GetBestMasterName);
            }
        }
    }
}
