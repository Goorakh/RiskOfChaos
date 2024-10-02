using BepInEx.Configuration;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectUtils.World.Spawn;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosEffect("spawn_random_boss", DefaultSelectionWeight = 0.8f)]
    public sealed class SpawnRandomBoss : GenericDirectorSpawnEffect<CharacterSpawnCard>
    {
        class BossSpawnEntry : SpawnCardEntry
        {
            public BossSpawnEntry(CharacterSpawnCard[] items, float weight) : base(items, weight)
            {
            }
            public BossSpawnEntry(string[] spawnCardPaths, float weight) : base(spawnCardPaths, weight)
            {
            }

            public BossSpawnEntry(CharacterSpawnCard item, float weight) : base(item, weight)
            {
            }
            public BossSpawnEntry(string spawnCardPath, float weight) : base(spawnCardPath, weight)
            {
            }

            protected override bool isItemAvailable(CharacterSpawnCard spawnCard)
            {
                if (spawnCard is MultiCharacterSpawnCard multiCharacterSpawnCard)
                {
                    GameObject[] masterPrefabs = multiCharacterSpawnCard.masterPrefabs;
                    return masterPrefabs != null && masterPrefabs.Length > 0 && Array.TrueForAll(masterPrefabs, isPrefabAvailable) && multiCharacterSpawnCard.HasValidSpawnLocation();
                }
                else
                {
                    return base.isItemAvailable(spawnCard);
                }
            }

            protected override bool isPrefabAvailable(GameObject prefab)
            {
                return base.isPrefabAvailable(prefab) && ExpansionUtils.IsCharacterMasterExpansionAvailable(prefab);
            }
        }

        static new BossSpawnEntry loadBasicSpawnEntry(string addressablePath, float weight = 1f)
        {
            return new BossSpawnEntry(addressablePath, weight);
        }

        static new BossSpawnEntry loadBasicSpawnEntry(string[] addressablePaths, float weight = 1f)
        {
            return new BossSpawnEntry(addressablePaths, weight);
        }

        static BossSpawnEntry[] _bossSpawnEntries;

        static readonly GameObject _bossCombatSquadPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Core/BossCombatSquad.prefab").WaitForCompletion();

        static InteractableSpawnCard _geodeSpawnCard;

        [SystemInitializer(typeof(CustomSpawnCards))]
        static void Init()
        {
            _bossSpawnEntries = [
                loadBasicSpawnEntry("RoR2/Base/Beetle/cscBeetleQueen.asset", 1f),
                loadBasicSpawnEntry("RoR2/Base/Brother/cscBrother.asset", 0.7f),
                loadBasicSpawnEntry("RoR2/Base/Brother/cscBrotherHurt.asset", 0.5f),
                loadBasicSpawnEntry("RoR2/Base/ClayBoss/cscClayBoss.asset", 1f),
                loadBasicSpawnEntry("RoR2/Base/ElectricWorm/cscElectricWorm.asset", 0.75f),
                loadBasicSpawnEntry("RoR2/Base/Grandparent/cscGrandparent.asset", 1f),
                loadBasicSpawnEntry("RoR2/Base/Gravekeeper/cscGravekeeper.asset", 1f),
                loadBasicSpawnEntry("RoR2/Base/ImpBoss/cscImpBoss.asset", 1f),
                loadBasicSpawnEntry("RoR2/Base/MagmaWorm/cscMagmaWorm.asset", 0.85f),
                loadBasicSpawnEntry("RoR2/Base/RoboBallBoss/cscRoboBallBoss.asset", 1f),
                loadBasicSpawnEntry("RoR2/Base/RoboBallBoss/cscSuperRoboBallBoss.asset", 1f),
                loadBasicSpawnEntry("RoR2/Base/Scav/cscScavBoss.asset", 0.9f),
                loadBasicSpawnEntry("RoR2/Base/ScavLunar/cscScavLunar.asset", 0.7f),
                loadBasicSpawnEntry("RoR2/Base/Titan/cscTitanBlackBeach.asset", 1f),
                loadBasicSpawnEntry("RoR2/Base/Titan/cscTitanGold.asset", 0.9f),
                loadBasicSpawnEntry("RoR2/Base/Vagrant/cscVagrant.asset", 1f),
                loadBasicSpawnEntry("RoR2/Junk/BrotherGlass/cscBrotherGlass.asset", 0.8f),
                loadBasicSpawnEntry("RoR2/DLC1/MajorAndMinorConstruct/cscMegaConstruct.asset", 1f),
                loadBasicSpawnEntry("RoR2/DLC1/VoidRaidCrab/cscMiniVoidRaidCrabPhase1.asset", 0.4f),
                loadBasicSpawnEntry("RoR2/DLC1/VoidRaidCrab/cscMiniVoidRaidCrabPhase2.asset", 0.4f),
                loadBasicSpawnEntry("RoR2/DLC1/VoidRaidCrab/cscMiniVoidRaidCrabPhase3.asset", 0.2f),
                loadBasicSpawnEntry("RoR2/DLC1/VoidMegaCrab/cscVoidMegaCrab.asset", 0.6f),
                loadBasicSpawnEntry("RoR2/DLC2/FalseSonBoss/cscFalseSonBoss.asset", 0.25f),
                loadBasicSpawnEntry("RoR2/DLC2/FalseSonBoss/cscFalseSonBossLunarShard.asset", 0.25f),
                loadBasicSpawnEntry("RoR2/DLC2/FalseSonBoss/cscFalseSonBossBrokenLunarShard.asset", 0.1f),
            ];

            _geodeSpawnCard = CustomSpawnCards.iscGeodeFixed;
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
            return areAnyAvailable(_bossSpawnEntries);
        }

        CharacterSpawnCard _selectedSpawnCard;
        Loadout _loadout;

        public override void OnPreStartServer()
        {
            base.OnPreStartServer();

            _selectedSpawnCard = getItemToSpawn(_bossSpawnEntries, RNG);
            _loadout = LoadoutUtils.GetRandomLoadoutFor(_selectedSpawnCard, RNG);
        }

        public override void OnStart()
        {
            DirectorPlacementRule placementRule = SpawnUtils.GetPlacementRule_AtRandomPlayerNearestNode(RNG, 30f, float.PositiveInfinity);

            DirectorSpawnRequest spawnRequest = new DirectorSpawnRequest(_selectedSpawnCard, placementRule, RNG)
            {
                teamIndexOverride = TeamIndex.Monster
            };

            CombatSquad bossCombatSquad;
            if (_bossCombatSquadPrefab)
            {
                GameObject bossCombatSquadObj = GameObject.Instantiate(_bossCombatSquadPrefab);

                BossGroup bossGroup = bossCombatSquadObj.GetComponent<BossGroup>();
                bossGroup.dropPosition = null; // Don't drop an item

                bossCombatSquad = bossCombatSquadObj.GetComponent<CombatSquad>();

                NetworkServer.Spawn(bossCombatSquadObj);
            }
            else
            {
                bossCombatSquad = null;
            }

            bool shouldSpawnGeodes = false;

            spawnRequest.onSpawnedServer = result =>
            {
                if (!result.success)
                    return;

                CharacterMaster master = result.spawnedInstance.GetComponent<CharacterMaster>();
                if (!master)
                    return;

                if (_loadout != null)
                {
                    master.SetLoadoutServer(_loadout);
                }

                if (RNG.nextNormalizedFloat <= _eliteChance.Value)
                {
                    EquipmentIndex eliteEquipmentIndex = EliteUtils.SelectEliteEquipment(RNG, _allowDirectorUnavailableElites.Value);

                    Inventory inventory = master.inventory;
                    if (inventory && inventory.GetEquipmentIndex() == EquipmentIndex.None)
                    {
                        inventory.SetEquipmentIndex(eliteEquipmentIndex);
                    }
                }

                if (bossCombatSquad)
                {
                    bossCombatSquad.AddMember(master);
                }

                if (!shouldSpawnGeodes && master.masterIndex == MasterCatalog.FindMasterIndex("FalseSonBossLunarShardBrokenMaster"))
                {
                    shouldSpawnGeodes = true;
                }
            };

            spawnRequest.SpawnWithFallbackPlacement(SpawnUtils.GetBestValidRandomPlacementRule());

            if (shouldSpawnGeodes && _geodeSpawnCard)
            {
                const float GEODE_MIN_SPAWN_DISTANCE = 5f;
                const float GEODE_MAX_SPAWN_DISTANCE = 100f;

                int playerGeodeSpawnsAttempted = 0;
                int spawnedGeodes = 0;

                foreach (CharacterBody playerBody in PlayerUtils.GetAllPlayerBodies(true))
                {
                    DirectorPlacementRule geodePlacementRule = new DirectorPlacementRule
                    {
                        placementMode = DirectorPlacementRule.PlacementMode.Approximate,
                        position = playerBody.footPosition,
                        minDistance = GEODE_MIN_SPAWN_DISTANCE,
                        maxDistance = GEODE_MAX_SPAWN_DISTANCE / 2f
                    };

                    DirectorSpawnRequest geodeSpawnRequest = new DirectorSpawnRequest(_geodeSpawnCard, geodePlacementRule, RNG.Branch());

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
                    DirectorPlacementRule geodePlacementRule = SpawnUtils.GetPlacementRule_AtRandomPlayerApproximate(RNG, GEODE_MIN_SPAWN_DISTANCE, GEODE_MAX_SPAWN_DISTANCE);
                    DirectorSpawnRequest geodeSpawnRequest = new DirectorSpawnRequest(_geodeSpawnCard, geodePlacementRule, RNG.Branch());

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
