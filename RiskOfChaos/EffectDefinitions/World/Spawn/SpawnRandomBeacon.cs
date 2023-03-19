using EntityStates.Captain.Weapon;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.Projectile;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosEffect("spawn_random_beacon")]
    public sealed class SpawnRandomBeacon : BaseEffect
    {
        static readonly List<GameObject> _beaconPrefabs = new List<GameObject>();

        [SystemInitializer]
        static void Init()
        {
            static void loadBeaconPrefab(string path)
            {
                AsyncOperationHandle<GameObject> loadBeaconHandle = Addressables.LoadAssetAsync<GameObject>(path);
                loadBeaconHandle.Completed += handle =>
                {
                    _beaconPrefabs.Add(handle.Result);
                };
            }

            loadBeaconPrefab("RoR2/Base/Captain/CaptainSupplyDrop, EquipmentRestock.prefab");
            loadBeaconPrefab("RoR2/Base/Captain/CaptainSupplyDrop, Hacking.prefab");
            loadBeaconPrefab("RoR2/Base/Captain/CaptainSupplyDrop, Healing.prefab");
            loadBeaconPrefab("RoR2/Base/Captain/CaptainSupplyDrop, Plating.prefab");
            loadBeaconPrefab("RoR2/Base/Captain/CaptainSupplyDrop, Shocking.prefab");
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _beaconPrefabs != null && _beaconPrefabs.Count > 0;
        }

        public override void OnStart()
        {
            GameObject beaconPrefab = RNG.NextElementUniform(_beaconPrefabs);

            foreach (CharacterBody playerBody in PlayerUtils.GetAllPlayerBodies(true))
            {
                GameObject beacon = GameObject.Instantiate(beaconPrefab, playerBody.footPosition, playerBody.GetRotation());

                beacon.GetComponent<TeamFilter>().teamIndex = playerBody.teamComponent.teamIndex;
                beacon.GetComponent<GenericOwnership>().ownerObject = playerBody.gameObject;

                ProjectileDamage damage = beacon.GetComponent<ProjectileDamage>();
                damage.crit = playerBody.RollCrit();
                damage.damage = playerBody.damage * CallSupplyDropBase.impactDamageCoefficient;
                damage.damageColorIndex = DamageColorIndex.Default;
                damage.force = CallSupplyDropBase.impactDamageForce;
                damage.damageType = DamageType.Generic;

                NetworkServer.Spawn(beacon);
            }
        }
    }
}
