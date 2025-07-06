using EntityStates.Captain.Weapon;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.ContentManagement;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosEffect("spawn_random_beacon")]
    public sealed class SpawnRandomBeacon : NetworkBehaviour
    {
        static readonly SpawnPool<GameObject> _beaconPool = new SpawnPool<GameObject>();

        [SystemInitializer]
        static void Init()
        {
            _beaconPool.EnsureCapacity(5);

            _beaconPool.AddAssetEntry(AddressableGuids.RoR2_Base_Captain_CaptainSupplyDrop_EquipmentRestock_prefab, new SpawnPoolEntryParameters(1f));
            _beaconPool.AddAssetEntry(AddressableGuids.RoR2_Base_Captain_CaptainSupplyDrop_Hacking_prefab, new SpawnPoolEntryParameters(1f));
            _beaconPool.AddAssetEntry(AddressableGuids.RoR2_Base_Captain_CaptainSupplyDrop_Healing_prefab, new SpawnPoolEntryParameters(1f));
            _beaconPool.AddAssetEntry(AddressableGuids.RoR2_Base_Captain_CaptainSupplyDrop_Plating_prefab, new SpawnPoolEntryParameters(1f));
            _beaconPool.AddAssetEntry(AddressableGuids.RoR2_Base_Captain_CaptainSupplyDrop_Shocking_prefab, new SpawnPoolEntryParameters(1f));
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _beaconPool.AnyAvailable;
        }

        ChaosEffectComponent _effectComponent;

        Xoroshiro128Plus _rng;

        AssetOrDirectReference<GameObject> _beaconRef;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        void OnDestroy()
        {
            _beaconRef?.Reset();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            _rng = new Xoroshiro128Plus(_effectComponent.Rng.nextUlong);

            _beaconRef = _beaconPool.PickRandomEntry(_rng);
            _beaconRef.CallOnLoaded(onBeaconPrefabLoaded);
        }

        [Server]
        void onBeaconPrefabLoaded(GameObject beaconPrefab)
        {
            foreach (PlayerCharacterMasterController playerMaster in PlayerCharacterMasterController.instances)
            {
                if (!playerMaster.isConnected)
                    continue;

                CharacterMaster master = playerMaster.master;
                if (!master || master.IsDeadAndOutOfLivesServer())
                    continue;

                CharacterBody body = master.GetBody();
                if (!body)
                    continue;

                Vector3 spawnPosition = body.footPosition;

                Vector3 up = VectorUtils.Spread(SpawnUtils.GetEnvironmentNormalAtPoint(spawnPosition), 5f, _rng);

                Quaternion rotation = QuaternionUtils.PointLocalDirectionAt(Vector3.up, up);

                GameObject beacon = Instantiate(beaconPrefab, spawnPosition, rotation);

                TeamFilter teamFilter = beacon.GetComponent<TeamFilter>();
                teamFilter.teamIndex = body.teamComponent.teamIndex;

                GenericOwnership genericOwnership = beacon.GetComponent<GenericOwnership>();
                genericOwnership.ownerObject = body.gameObject;

                ProjectileDamage damage = beacon.GetComponent<ProjectileDamage>();
                damage.damage = body.damage * CallSupplyDropBase.impactDamageCoefficient;
                damage.damageColorIndex = DamageColorIndex.Default;
                damage.force = CallSupplyDropBase.impactDamageForce;
                damage.damageType = DamageType.Generic;

                NetworkServer.Spawn(beacon);
            }
        }
    }
}
