using RiskOfChaos.EffectHandling;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosEffect("Meteor")]
    public class Meteor : BaseEffect
    {
        public override void OnStart()
        {
            MeteorStormController meteorController = UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/MeteorStorm")).GetComponent<MeteorStormController>();
            meteorController.ownerDamage = 100f;
            meteorController.isCrit = false;
            NetworkServer.Spawn(meteorController.gameObject);
        }
    }
}
