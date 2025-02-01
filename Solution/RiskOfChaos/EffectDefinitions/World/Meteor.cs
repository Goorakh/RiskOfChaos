using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosEffect("meteor")]
    public sealed class Meteor : MonoBehaviour
    {
        [AddressableReference("RoR2/Base/Meteor/MeteorStorm.prefab")]
        static readonly GameObject _meteorStormPrefab;

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
