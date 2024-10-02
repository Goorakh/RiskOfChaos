using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosEffect("meteor")]
    public sealed class Meteor : BaseEffect
    {
        public override void OnStart()
        {
            MeteorStormController meteorController = UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/MeteorStorm")).GetComponent<MeteorStormController>();
            meteorController.ownerDamage = 40f * Run.instance.teamlessDamageCoefficient;
            meteorController.isCrit = false;
            NetworkServer.Spawn(meteorController.gameObject);
        }
    }
}
