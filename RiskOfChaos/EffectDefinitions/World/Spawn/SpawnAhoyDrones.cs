using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RoR2;
using RoR2.Navigation;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosEffect("spawn_ahoy_drones", EffectRepetitionWeightCalculationMode = EffectActivationCountMode.PerRun, EffectWeightReductionPercentagePerActivation = 10f)]
    public sealed class SpawnAhoyDrones : BaseEffect
    {
        static readonly CharacterSpawnCard _equipmentDroneSpawnCard;

        static SpawnAhoyDrones()
        {
            _equipmentDroneSpawnCard = ScriptableObject.CreateInstance<CharacterSpawnCard>();
            _equipmentDroneSpawnCard.name = "cscEquipmentDrone";
            _equipmentDroneSpawnCard.sendOverNetwork = true;
            _equipmentDroneSpawnCard.nodeGraphType = MapNodeGroup.GraphType.Air;
            _equipmentDroneSpawnCard.forbiddenFlags = NodeFlags.NoCharacterSpawn;
            _equipmentDroneSpawnCard.eliteRules = SpawnCard.EliteRules.ArtifactOnly;

            _equipmentDroneSpawnCard.noElites = true;
            _equipmentDroneSpawnCard.forbiddenAsBoss = true;

            _equipmentDroneSpawnCard.prefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Drones/EquipmentDroneMaster.prefab").WaitForCompletion();

            if (_equipmentDroneSpawnCard.prefab &&
                _equipmentDroneSpawnCard.prefab.TryGetComponent(out CharacterMaster masterPrefab) &&
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
        }

        [EffectConfig]
        static readonly ConfigHolder<int> _droneSpawnCount =
            ConfigFactory<int>.CreateConfig("Spawn Count", 3)
                              .Description("How many drones should be spawned")
                              .OptionConfig(new IntSliderConfig
                              {
                                  min = 1,
                                  max = 15
                              })
                              .ValueConstrictor(CommonValueConstrictors.GreaterThanOrEqualTo(1))
                              .Build();

        [EffectCanActivate]
        static bool CanActivate(in EffectCanActivateContext context)
        {
            return ExpansionUtils.DLC1Enabled && DirectorCore.instance && _equipmentDroneSpawnCard.HasValidSpawnLocation() && (!context.IsNow || PlayerUtils.GetAllPlayerBodies(true).Any());
        }

        public override void OnStart()
        {
            CharacterBody[] playerBodies = PlayerUtils.GetAllPlayerBodies(true).ToArray();
            Util.ShuffleArray(playerBodies, RNG.Branch());

            int spawnsRemaining = _droneSpawnCount.Value;
            foreach (CharacterBody playerBody in playerBodies)
            {
                spawnDroneAt(playerBody, RNG.Branch());

                if (--spawnsRemaining <= 0)
                    return;
            }

            for (int i = 0; i < spawnsRemaining; i++)
            {
                spawnDroneAt(RNG.NextElementUniform(playerBodies), RNG.Branch());
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
                teamIndexOverride = ownerBody.teamComponent.teamIndex,
                ignoreTeamMemberLimit = true
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
