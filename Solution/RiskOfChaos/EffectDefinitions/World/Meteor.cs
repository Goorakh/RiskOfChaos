using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosEffect("meteor")]
    public sealed class Meteor : MonoBehaviour
    {
        static GameObject _meteorStormPrefab;

        [SystemInitializer]
        static void Init()
        {
            AsyncOperationHandle<GameObject> meteorStormLoad = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Meteor/MeteorStorm.prefab");
            meteorStormLoad.OnSuccess(p => _meteorStormPrefab = p);
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _meteorStormPrefab;
        }

        void Start()
        {
            if (!NetworkServer.active)
                return;

            MeteorStormController meteorController = Instantiate(_meteorStormPrefab).GetComponent<MeteorStormController>();
            meteorController.ownerDamage = 40f * Run.instance.teamlessDamageCoefficient;
            meteorController.isCrit = false;
            NetworkServer.Spawn(meteorController.gameObject);
        }
    }
}
