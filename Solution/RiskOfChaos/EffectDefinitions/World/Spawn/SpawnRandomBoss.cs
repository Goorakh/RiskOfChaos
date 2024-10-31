using BepInEx.Configuration;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.EffectUtils.World.Spawn;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosEffect("spawn_random_boss", DefaultSelectionWeight = 0.8f)]
    public sealed class SpawnRandomBoss : NetworkBehaviour
    {
        static GameObject _bossCombatSquadPrefab;

        static InteractableSpawnCard _geodeSpawnCard;

        static readonly SpawnPool<CharacterSpawnCard> _spawnPool = new SpawnPool<CharacterSpawnCard>
        {
            RequiredExpansionsProvider = SpawnPoolUtils.CharacterSpawnCardExpansionsProvider
        };

        [SystemInitializer(typeof(CustomSpawnCards))]
        static void Init()
        {
            AsyncOperationHandle<GameObject> bossCombatSquadLoad = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Core/BossCombatSquad.prefab");
            bossCombatSquadLoad.OnSuccess(bossCombatSquadPrefab => _bossCombatSquadPrefab = bossCombatSquadPrefab);

            _geodeSpawnCard = CustomSpawnCards.iscGeodeFixed;

            _spawnPool.EnsureCapacity(25);

            _spawnPool.AddAssetEntry("RoR2/Base/Beetle/cscBeetleQueen.asset", 1f);
            _spawnPool.AddAssetEntry("RoR2/Base/Brother/cscBrother.asset", 0.7f);
            _spawnPool.AddAssetEntry("RoR2/Base/Brother/cscBrotherHurt.asset", 0.5f);
            _spawnPool.AddAssetEntry("RoR2/Base/ClayBoss/cscClayBoss.asset", 1f);
            _spawnPool.AddAssetEntry("RoR2/Base/ElectricWorm/cscElectricWorm.asset", 0.75f);
            _spawnPool.AddAssetEntry("RoR2/Base/Grandparent/cscGrandparent.asset", 1f);
            _spawnPool.AddAssetEntry("RoR2/Base/Gravekeeper/cscGravekeeper.asset", 1f);
            _spawnPool.AddAssetEntry("RoR2/Base/ImpBoss/cscImpBoss.asset", 1f);
            _spawnPool.AddAssetEntry("RoR2/Base/MagmaWorm/cscMagmaWorm.asset", 0.85f);
            _spawnPool.AddAssetEntry("RoR2/Base/RoboBallBoss/cscRoboBallBoss.asset", 1f);
            _spawnPool.AddAssetEntry("RoR2/Base/RoboBallBoss/cscSuperRoboBallBoss.asset", 1f);
            _spawnPool.AddAssetEntry("RoR2/Base/Scav/cscScavBoss.asset", 0.9f);
            _spawnPool.AddAssetEntry("RoR2/Base/ScavLunar/cscScavLunar.asset", 0.7f);
            _spawnPool.AddAssetEntry("RoR2/Base/Titan/cscTitanBlackBeach.asset", 1f);
            _spawnPool.AddAssetEntry("RoR2/Base/Titan/cscTitanGold.asset", 0.9f);
            _spawnPool.AddAssetEntry("RoR2/Base/Vagrant/cscVagrant.asset", 1f);
            _spawnPool.AddAssetEntry("RoR2/Junk/BrotherGlass/cscBrotherGlass.asset", 0.8f);
            _spawnPool.AddAssetEntry("RoR2/DLC1/MajorAndMinorConstruct/cscMegaConstruct.asset", 1f);

            _spawnPool.AddGroupedEntries([
                _spawnPool.LoadEntry("RoR2/DLC1/VoidRaidCrab/cscMiniVoidRaidCrabPhase1.asset", 1f),
                _spawnPool.LoadEntry("RoR2/DLC1/VoidRaidCrab/cscMiniVoidRaidCrabPhase2.asset", 0.9f),
                _spawnPool.LoadEntry("RoR2/DLC1/VoidRaidCrab/cscMiniVoidRaidCrabPhase3.asset", 0.75f),
            ], 0.85f);

            _spawnPool.AddAssetEntry("RoR2/DLC1/VoidMegaCrab/cscVoidMegaCrab.asset", 0.6f);

            _spawnPool.AddGroupedEntries([
                _spawnPool.LoadEntry("RoR2/DLC2/FalseSonBoss/cscFalseSonBoss.asset", 1f),
                _spawnPool.LoadEntry("RoR2/DLC2/FalseSonBoss/cscFalseSonBossLunarShard.asset", 1f),
                _spawnPool.LoadEntry("RoR2/DLC2/FalseSonBoss/cscFalseSonBossBrokenLunarShard.asset", 0.5f),
            ], 0.7f);
        }

        [EffectConfig]
        static readonly ConfigHolder<float> _eliteChance =
            ConfigFactory<float>.CreateConfig("Elite Chance", 0.15f)
                                .Description("The likelyhood for the spawned boss to be an elite")
                                .AcceptableValues(new AcceptableValueRange<float>(0f, 1f))
                                .OptionConfig(new StepSliderConfig
                                {
                                    FormatString = "{0:P0}",
                                    min = 0f,
                                    max = 1f,
                                    increment = 0.01f
                                })
                                .Build();

        [EffectConfig]
        static readonly ConfigHolder<bool> _allowDirectorUnavailableElites =
            ConfigFactory<bool>.CreateConfig("Ignore Elite Selection Rules", true)
                               .Description("If the effect should ignore normal elite selection rules. If enabled, any elite type can be selected, if disabled, only the elite types that can currently be spawned on the stage can be selected")
                               .OptionConfig(new CheckBoxConfig())
                               .Build();

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _spawnPool.AnyAvailable;
        }

        ChaosEffectComponent _effectComponent;

        Xoroshiro128Plus _rng;

        CharacterSpawnCard _selectedSpawnCard;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            _rng = new Xoroshiro128Plus(_effectComponent.Rng.nextUlong);

            _selectedSpawnCard = _spawnPool.PickRandomEntry(_rng);
        }

        void Start()
        {
            if (!NetworkServer.active)
                return;

            DirectorPlacementRule placementRule = SpawnUtils.GetPlacementRule_AtRandomPlayerNearestNode(_rng, 30f, float.PositiveInfinity);

            DirectorSpawnRequest spawnRequest = new DirectorSpawnRequest(_selectedSpawnCard, placementRule, _rng)
            {
                teamIndexOverride = TeamIndex.Monster,
                ignoreTeamMemberLimit = true
            };

            List<CharacterMaster> spawnedMasters = [];
            bool shouldSpawnGeodes = false;

            spawnRequest.onSpawnedServer = result =>
            {
                if (!result.success)
                    return;

                if (result.spawnedInstance.TryGetComponent(out CharacterMaster master))
                {
                    CombatCharacterSpawnHelper.SetupSpawnedCombatCharacter(master, _rng);
                    CombatCharacterSpawnHelper.TryGrantEliteAspect(master, _rng, _eliteChance.Value, _allowDirectorUnavailableElites.Value, true);

                    spawnedMasters.Add(master);

                    if (!shouldSpawnGeodes && master.masterIndex == MasterCatalog.FindMasterIndex("FalseSonBossLunarShardBrokenMaster"))
                    {
                        shouldSpawnGeodes = true;
                    }
                }
            };

            spawnRequest.SpawnWithFallbackPlacement(SpawnUtils.GetBestValidRandomPlacementRule());

            if (spawnedMasters.Count > 0 && _bossCombatSquadPrefab)
            {
                GameObject bossCombatSquadObj = Instantiate(_bossCombatSquadPrefab);

                BossGroup bossGroup = bossCombatSquadObj.GetComponent<BossGroup>();
                bossGroup.dropPosition = null; // Don't drop an item

                CombatSquad bossCombatSquad = bossCombatSquadObj.GetComponent<CombatSquad>();
                foreach (CharacterMaster master in spawnedMasters)
                {
                    bossCombatSquad.AddMember(master);
                }

                NetworkServer.Spawn(bossCombatSquadObj);
            }

            if (shouldSpawnGeodes && _geodeSpawnCard)
            {
                const float GEODE_MIN_SPAWN_DISTANCE = 5f;
                const float GEODE_MAX_SPAWN_DISTANCE = 100f;

                const float GEODE_CLOSE_MIN_SPAWN_DISTANCE = GEODE_MIN_SPAWN_DISTANCE;
                const float GEODE_CLOSE_MAX_SPAWN_DISTANCE = GEODE_MAX_SPAWN_DISTANCE / 2f;

                int playerGeodeSpawnsAttempted = 0;
                int spawnedGeodes = 0;

                foreach (PlayerCharacterMasterController playerMaster in PlayerCharacterMasterController.instances)
                {
                    if (!playerMaster.isConnected)
                        continue;

                    CharacterMaster master = playerMaster.master;
                    if (!master || master.IsDeadAndOutOfLivesServer())
                        continue;

                    if (!master.TryGetBodyPosition(out Vector3 bodyPosition))
                        continue;

                    DirectorPlacementRule geodePlacementRule = new DirectorPlacementRule
                    {
                        placementMode = DirectorPlacementRule.PlacementMode.Approximate,
                        position = bodyPosition,
                        minDistance = GEODE_CLOSE_MIN_SPAWN_DISTANCE,
                        maxDistance = GEODE_CLOSE_MAX_SPAWN_DISTANCE
                    };

                    DirectorSpawnRequest geodeSpawnRequest = new DirectorSpawnRequest(_geodeSpawnCard, geodePlacementRule, _rng);

                    playerGeodeSpawnsAttempted++;
                    if (DirectorCore.instance.TrySpawnObject(geodeSpawnRequest))
                    {
                        spawnedGeodes++;
                    }
                }

                const int EXTRA_GEODE_COUNT = 5;

                int missedPlayerSpawnGeodes = Math.Max(0, playerGeodeSpawnsAttempted - spawnedGeodes);
                int additionalGeodeSpawnCount = missedPlayerSpawnGeodes + EXTRA_GEODE_COUNT;

                for (int i = 0; i < additionalGeodeSpawnCount; i++)
                {
                    DirectorPlacementRule geodePlacementRule = SpawnUtils.GetPlacementRule_AtRandomPlayerApproximate(_rng, GEODE_MIN_SPAWN_DISTANCE, GEODE_MAX_SPAWN_DISTANCE);
                    DirectorSpawnRequest geodeSpawnRequest = new DirectorSpawnRequest(_geodeSpawnCard, geodePlacementRule, _rng);

                    if (geodeSpawnRequest.SpawnWithFallbackPlacement(SpawnUtils.GetBestValidRandomPlacementRule()))
                    {
                        spawnedGeodes++;
                    }
                }
                
#if DEBUG
                Log.Debug($"Spawned {spawnedGeodes} geode(s)");
#endif
            }
        }
    }
}
