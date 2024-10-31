using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RoR2;
using RoR2.Navigation;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosEffect("spawn_ahoy_drones")]
    public sealed class SpawnAhoyDrones : NetworkBehaviour
    {
        static CharacterSpawnCard _equipmentDroneSpawnCard;

        [SystemInitializer]
        static void Init()
        {
            AsyncOperationHandle<GameObject> equipmentDroneMasterLoad = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Drones/EquipmentDroneMaster.prefab");
            equipmentDroneMasterLoad.OnSuccess(equipmentDroneMasterPrefab =>
            {
                _equipmentDroneSpawnCard = ScriptableObject.CreateInstance<CharacterSpawnCard>();
                _equipmentDroneSpawnCard.name = "cscEquipmentDrone";
                _equipmentDroneSpawnCard.prefab = equipmentDroneMasterPrefab;
                _equipmentDroneSpawnCard.sendOverNetwork = true;
                _equipmentDroneSpawnCard.nodeGraphType = MapNodeGroup.GraphType.Air;
                _equipmentDroneSpawnCard.forbiddenFlags = NodeFlags.NoCharacterSpawn;

                _equipmentDroneSpawnCard.equipmentToGrant = [
                    DLC1Content.Equipment.BossHunterConsumed
                ];

                if (_equipmentDroneSpawnCard.prefab.TryGetComponent(out CharacterMaster masterPrefab) &&
                    masterPrefab.bodyPrefab &&
                    masterPrefab.bodyPrefab.TryGetComponent(out CharacterBody bodyPrefab))
                {
                    _equipmentDroneSpawnCard.hullSize = bodyPrefab.hullClassification;

#if DEBUG
                    Log.Debug($"Set SpawnCard hull size to: {_equipmentDroneSpawnCard.hullSize}");
#endif
                }
                else
                {
                    Log.Error("Failed to get equipment drone hull size");
                    _equipmentDroneSpawnCard.hullSize = HullClassification.Human;
                }
            });
        }

        [EffectConfig]
        static readonly ConfigHolder<int> _droneSpawnCount =
            ConfigFactory<int>.CreateConfig("Spawn Count", 3)
                              .Description("How many drones should be spawned")
                              .AcceptableValues(new AcceptableValueMin<int>(1))
                              .OptionConfig(new IntFieldConfig { Min = 1 })
                              .Build();

        [EffectCanActivate]
        static bool CanActivate()
        {
            return ExpansionUtils.DLC1Enabled && DirectorCore.instance && _equipmentDroneSpawnCard.HasValidSpawnLocation();
        }

        ChaosEffectComponent _effectComponent;

        Xoroshiro128Plus _rng;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            _rng = new Xoroshiro128Plus(_effectComponent.Rng.nextUlong);
        }

        void Start()
        {
            if (!NetworkServer.active)
                return;

            List<CharacterMaster> playerMasters = new List<CharacterMaster>(PlayerCharacterMasterController.instances.Count);
            foreach (PlayerCharacterMasterController playerMaster in PlayerCharacterMasterController.instances)
            {
                if (!playerMaster.isConnected)
                    continue;

                CharacterMaster master = playerMaster.master;
                if (!master || master.IsDeadAndOutOfLivesServer() || !master.GetBody())
                    continue;

                playerMasters.Add(master);
            }

            Util.ShuffleList(playerMasters, _rng);

            int spawnsRemaining = _droneSpawnCount.Value;
            foreach (CharacterMaster playerMaster in playerMasters)
            {
                spawnDroneAt(playerMaster, _rng);

                spawnsRemaining--;
                if (spawnsRemaining <= 0)
                    break;
            }

            for (int i = 0; i < spawnsRemaining; i++)
            {
                spawnDroneAt(_rng.NextElementUniform(playerMasters), _rng);
            }
        }

        static void spawnDroneAt(CharacterMaster ownerMaster, Xoroshiro128Plus rng)
        {
            GameObject bodyObject = ownerMaster.GetBodyObject();
            CharacterBody body = ownerMaster.GetBody();

            DirectorPlacementRule placementRule = new DirectorPlacementRule
            {
                position = body.corePosition,
                placementMode = DirectorPlacementRule.PlacementMode.NearestNode
            };

            DirectorSpawnRequest spawnRequest = new DirectorSpawnRequest(_equipmentDroneSpawnCard, placementRule, rng)
            {
                ignoreTeamMemberLimit = true,
                summonerBodyObject = bodyObject,
                teamIndexOverride = ownerMaster.teamIndex,
            };

            spawnRequest.SpawnWithFallbackPlacement(SpawnUtils.GetBestValidRandomPlacementRule());
        }
    }
}
