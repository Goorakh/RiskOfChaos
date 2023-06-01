using BepInEx.Configuration;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2;
using RoR2.Navigation;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosEffect("spawn_ahoy_drones", EffectRepetitionWeightCalculationMode = EffectActivationCountMode.PerRun, EffectWeightReductionPercentagePerActivation = 10f)]
    public sealed class SpawnAhoyDrones : BaseEffect
    {
        [InitEffectInfo]
        static readonly ChaosEffectInfo _effectInfo;

        static CharacterSpawnCard _equipmentDroneSpawnCard;

        [SystemInitializer]
        static void InitSpawnCardPrefab()
        {
            AsyncOperationHandle<GameObject> loadEquipmentDronePrefabHandle = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Drones/EquipmentDroneMaster.prefab");
            loadEquipmentDronePrefabHandle.Completed += handle =>
            {
                if (!handle.Result)
                {
                    Log.Error("Failed to load equipment drone prefab");
                    return;
                }

                _equipmentDroneSpawnCard = ScriptableObject.CreateInstance<CharacterSpawnCard>();

                _equipmentDroneSpawnCard.name = "cscEquipmentDrone";
                _equipmentDroneSpawnCard.sendOverNetwork = true;
                _equipmentDroneSpawnCard.nodeGraphType = MapNodeGroup.GraphType.Air;
                _equipmentDroneSpawnCard.forbiddenFlags = NodeFlags.NoCharacterSpawn;
                _equipmentDroneSpawnCard.eliteRules = SpawnCard.EliteRules.ArtifactOnly;

                _equipmentDroneSpawnCard.noElites = true;
                _equipmentDroneSpawnCard.forbiddenAsBoss = true;

                _equipmentDroneSpawnCard.prefab = handle.Result;

                if (handle.Result.TryGetComponent(out CharacterMaster masterPrefab) &&
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
                    Log.Warning("Failed to get equipment drone hull size");
                }
            };
        }

        static ConfigEntry<int> _spawnCountConfig;
        const int SPAWN_COUNT_DEFAULT_VALUE = 3;

        static int spawnCount
        {
            get
            {
                if (_spawnCountConfig == null)
                {
                    return SPAWN_COUNT_DEFAULT_VALUE;
                }
                else
                {
                    return Mathf.Max(_spawnCountConfig.Value, 1);
                }
            }
        }

        [SystemInitializer(typeof(ChaosEffectCatalog))]
        static void InitConfigs()
        {
            _spawnCountConfig = _effectInfo.BindConfig("Spawn Count", SPAWN_COUNT_DEFAULT_VALUE, new ConfigDescription("How many drones should be spawned"));

            addConfigOption(new IntSliderOption(_spawnCountConfig, new IntSliderConfig
            {
                min = 1,
                max = 15
            }));
        }

        [EffectCanActivate]
        static bool CanActivate(EffectCanActivateContext context)
        {
            return ExpansionUtils.DLC1Enabled && DirectorCore.instance && _equipmentDroneSpawnCard.HasValidSpawnLocation() && (!context.IsNow || PlayerUtils.GetAllPlayerBodies(true).Any());
        }

        public override void OnStart()
        {
            CharacterBody[] playerBodies = PlayerUtils.GetAllPlayerBodies(true).ToArray();
            Util.ShuffleArray(playerBodies, new Xoroshiro128Plus(RNG.nextUlong));

            int spawnsRemaining = spawnCount;
            foreach (CharacterBody playerBody in playerBodies)
            {
                spawnDroneAt(playerBody, new Xoroshiro128Plus(RNG.nextUlong));

                if (--spawnsRemaining <= 0)
                    return;
            }

            for (int i = 0; i < spawnsRemaining; i++)
            {
                spawnDroneAt(RNG.NextElementUniform(playerBodies), new Xoroshiro128Plus(RNG.nextUlong));
            }
        }

        static void spawnDroneAt(CharacterBody ownerBody, Xoroshiro128Plus rng)
        {
            DirectorPlacementRule placementRule = new DirectorPlacementRule
            {
                position = ownerBody.footPosition,
                placementMode = DirectorPlacementRule.PlacementMode.NearestNode
            };

            DirectorSpawnRequest spawnRequest = new DirectorSpawnRequest(_equipmentDroneSpawnCard, placementRule, rng)
            {
                summonerBodyObject = ownerBody.gameObject,
                teamIndexOverride = ownerBody.teamComponent.teamIndex
            };

            spawnRequest.onSpawnedServer = result =>
            {
                if (!result.success || !result.spawnedInstance)
                    return;

                if (result.spawnedInstance.TryGetComponent(out Inventory inventory))
                {
                    if (inventory.GetEquipmentIndex() == EquipmentIndex.None)
                    {
                        inventory.SetEquipmentIndex(DLC1Content.Equipment.BossHunterConsumed.equipmentIndex);
                    }
                }
            };

            DirectorCore.instance.TrySpawnObject(spawnRequest);
        }
    }
}
