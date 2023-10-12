using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
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

        [SystemInitializer]
        static void Init()
        {
            _bossSpawnEntries = new BossSpawnEntry[]
            {
                loadBasicSpawnEntry("RoR2/Base/Beetle/cscBeetleQueen.asset", 1f),
                loadBasicSpawnEntry("RoR2/Base/Brother/cscBrother.asset", 0.5f),
                loadBasicSpawnEntry("RoR2/Base/Brother/cscBrotherHurt.asset", 0.4f),
                loadBasicSpawnEntry("RoR2/Base/ClayBoss/cscClayBoss.asset", 1f),
                loadBasicSpawnEntry("RoR2/Base/ElectricWorm/cscElectricWorm.asset", 0.75f),
                loadBasicSpawnEntry("RoR2/Base/Grandparent/cscGrandparent.asset", 1f),
                loadBasicSpawnEntry("RoR2/Base/Gravekeeper/cscGravekeeper.asset", 1f),
                loadBasicSpawnEntry("RoR2/Base/ImpBoss/cscImpBoss.asset", 1f),
                loadBasicSpawnEntry("RoR2/Base/MagmaWorm/cscMagmaWorm.asset", 0.85f),
                loadBasicSpawnEntry("RoR2/Base/RoboBallBoss/cscRoboBallBoss.asset", 1f),
                loadBasicSpawnEntry("RoR2/Base/RoboBallBoss/cscSuperRoboBallBoss.asset", 1f),
                loadBasicSpawnEntry("RoR2/Base/Scav/cscScavBoss.asset", 0.9f),
                loadBasicSpawnEntry("RoR2/Base/ScavLunar/cscScavLunar.asset", 0.8f),
                loadBasicSpawnEntry("RoR2/Base/Titan/cscTitanBlackBeach.asset", 1f),
                loadBasicSpawnEntry("RoR2/Base/Titan/cscTitanGold.asset", 0.9f),
                loadBasicSpawnEntry("RoR2/Base/Vagrant/cscVagrant.asset", 1f),
                loadBasicSpawnEntry("RoR2/Junk/BrotherGlass/cscBrotherGlass.asset", 0.7f),
                loadBasicSpawnEntry("RoR2/DLC1/MajorAndMinorConstruct/cscMegaConstruct.asset", 1f),
                loadBasicSpawnEntry("RoR2/DLC1/VoidRaidCrab/cscMiniVoidRaidCrabPhase1.asset", 0.1f),
                loadBasicSpawnEntry("RoR2/DLC1/VoidRaidCrab/cscMiniVoidRaidCrabPhase2.asset", 0.1f),
                loadBasicSpawnEntry("RoR2/DLC1/VoidRaidCrab/cscMiniVoidRaidCrabPhase3.asset", 0.075f),
                loadBasicSpawnEntry("RoR2/DLC1/VoidMegaCrab/cscVoidMegaCrab.asset", 0.5f),
            };
        }

        [EffectConfig]
        static readonly ConfigHolder<float> _eliteChance =
            ConfigFactory<float>.CreateConfig("Elite Chance", 0.15f)
                                .Description("The likelyhood for the spawned boss to be an elite")
                                .OptionConfig(new StepSliderConfig
                                {
                                    formatString = "{0:P0}",
                                    min = 0f,
                                    max = 1f,
                                    increment = 0.01f
                                })
                                .ValueConstrictor(CommonValueConstrictors.Clamped01Float)
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
            DirectorPlacementRule placementRule = SpawnUtils.GetPlacementRule_AtRandomPlayerApproximate(RNG, 30f, 50f);

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
            };

            GameObject spawnedObject = DirectorCore.instance.TrySpawnObject(spawnRequest);
            if (!spawnedObject)
            {
                spawnRequest.placementRule = SpawnUtils.GetBestValidRandomPlacementRule();
                spawnedObject = DirectorCore.instance.TrySpawnObject(spawnRequest);
            }
        }
    }
}
