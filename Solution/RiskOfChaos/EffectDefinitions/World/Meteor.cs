using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.ContentManagement;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosEffect("meteor")]
    public sealed class Meteor : MonoBehaviour
    {
        void Start()
        {
            if (NetworkServer.active)
            {
                AddressableUtil.LoadTempAssetAsync<GameObject>(AddressableGuids.RoR2_Base_Meteor_MeteorStorm_prefab, AsyncReferenceHandleUnloadType.OnSceneUnload).OnSuccess(spawnMeteor);
            }
        }

        static void spawnMeteor(GameObject meteorStormPrefab)
        {
            MeteorStormController meteorController = Instantiate(meteorStormPrefab).GetComponent<MeteorStormController>();
            meteorController.ownerDamage = 40f * Run.instance.teamlessDamageCoefficient;
            meteorController.isCrit = false;
            NetworkServer.Spawn(meteorController.gameObject);
        }
    }
}
