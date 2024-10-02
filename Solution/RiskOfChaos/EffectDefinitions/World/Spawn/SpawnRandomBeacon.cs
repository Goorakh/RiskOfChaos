using EntityStates.Captain.Weapon;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosEffect("spawn_random_beacon")]
    public sealed class SpawnRandomBeacon : GenericSpawnEffect<GameObject>
    {
        static SpawnEntry[] _beaconSpawnEntries;

        [SystemInitializer]
        static void Init()
        {
            _beaconSpawnEntries = [
                loadBasicSpawnEntry("RoR2/Base/Captain/CaptainSupplyDrop, EquipmentRestock.prefab"),
                loadBasicSpawnEntry("RoR2/Base/Captain/CaptainSupplyDrop, Hacking.prefab"),
                loadBasicSpawnEntry("RoR2/Base/Captain/CaptainSupplyDrop, Healing.prefab"),
                loadBasicSpawnEntry("RoR2/Base/Captain/CaptainSupplyDrop, Plating.prefab"),
                loadBasicSpawnEntry("RoR2/Base/Captain/CaptainSupplyDrop, Shocking.prefab")
            ];
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return areAnyAvailable(_beaconSpawnEntries);
        }

        public override void OnStart()
        {
            GameObject beaconPrefab = getItemToSpawn(_beaconSpawnEntries, RNG);

            foreach (CharacterBody playerBody in PlayerUtils.GetAllPlayerBodies(true))
            {
                Vector3 spawnPosition = playerBody.footPosition;

                Quaternion rotation = QuaternionUtils.PointLocalDirectionAt(Vector3.up, SpawnUtils.GetEnvironmentNormalAtPoint(spawnPosition))
                                    * QuaternionUtils.RandomDeviation(5f, RNG);

                GameObject beacon = GameObject.Instantiate(beaconPrefab, spawnPosition, rotation);

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
