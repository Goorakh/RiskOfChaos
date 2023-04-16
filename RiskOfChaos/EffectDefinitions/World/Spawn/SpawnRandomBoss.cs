using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
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

            public BossSpawnEntry(CharacterSpawnCard item, float weight) : base(item, weight)
            {
            }

            protected override bool isItemAvailable(CharacterSpawnCard spawnCard)
            {
                if (spawnCard is MultiCharacterSpawnCard multiCharacterSpawnCard)
                {
                    GameObject[] masterPrefabs = multiCharacterSpawnCard.masterPrefabs;
                    return masterPrefabs != null && masterPrefabs.Length > 0 && Array.TrueForAll(masterPrefabs, isPrefabAvailable);
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

        static BossSpawnEntry[] _bossSpawnEntries;

        static readonly GameObject _bossCombatSquadPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Core/BossCombatSquad.prefab").WaitForCompletion();

        [SystemInitializer]
        static void Init()
        {
            static BossSpawnEntry getBossEntrySingle(string assetPath, float weight)
            {
                return new BossSpawnEntry(Addressables.LoadAssetAsync<CharacterSpawnCard>(assetPath).WaitForCompletion(), weight);
            }

            static BossSpawnEntry getBossEntryMany(string[] assetPaths, float weight)
            {
                CharacterSpawnCard[] spawnCards = Array.ConvertAll(assetPaths, path => Addressables.LoadAssetAsync<CharacterSpawnCard>(path).WaitForCompletion());
                return new BossSpawnEntry(spawnCards, weight);
            }

            _bossSpawnEntries = new BossSpawnEntry[]
            {
                getBossEntrySingle("RoR2/Base/Beetle/cscBeetleQueen.asset", 1f),
                getBossEntrySingle("RoR2/Base/Brother/cscBrother.asset", 0.5f),
                getBossEntrySingle("RoR2/Base/Brother/cscBrotherHurt.asset", 0.4f),
                getBossEntrySingle("RoR2/Base/ClayBoss/cscClayBoss.asset", 1f),
                getBossEntrySingle("RoR2/Base/ElectricWorm/cscElectricWorm.asset", 0.75f),
                getBossEntrySingle("RoR2/Base/Grandparent/cscGrandparent.asset", 1f),
                getBossEntrySingle("RoR2/Base/Gravekeeper/cscGravekeeper.asset", 1f),
                getBossEntrySingle("RoR2/Base/ImpBoss/cscImpBoss.asset", 1f),
                getBossEntrySingle("RoR2/Base/MagmaWorm/cscMagmaWorm.asset", 0.85f),
                getBossEntrySingle("RoR2/Base/RoboBallBoss/cscRoboBallBoss.asset", 1f),
                getBossEntrySingle("RoR2/Base/RoboBallBoss/cscSuperRoboBallBoss.asset", 1f),
                getBossEntrySingle("RoR2/Base/Scav/cscScavBoss.asset", 0.9f),
                getBossEntrySingle("RoR2/Base/ScavLunar/cscScavLunar.asset", 0.8f),

                getBossEntryMany(new string[]
                {
                    "RoR2/Base/Titan/cscTitanBlackBeach.asset",
                    "RoR2/Base/Titan/cscTitanDampCave.asset",
                    "RoR2/Base/Titan/cscTitanGolemPlains.asset",
                    "RoR2/Base/Titan/cscTitanGooLake.asset"
                }, 1f),

                getBossEntrySingle("RoR2/Base/Titan/cscTitanGold.asset", 0.9f),
                getBossEntrySingle("RoR2/Base/Vagrant/cscVagrant.asset", 1f),
                getBossEntrySingle("RoR2/Junk/BrotherGlass/cscBrotherGlass.asset", 0.7f),
                getBossEntrySingle("RoR2/DLC1/MajorAndMinorConstruct/cscMegaConstruct.asset", 1f),
                getBossEntrySingle("RoR2/DLC1/VoidRaidCrab/cscMiniVoidRaidCrabPhase1.asset", 0.1f),
                getBossEntrySingle("RoR2/DLC1/VoidRaidCrab/cscMiniVoidRaidCrabPhase2.asset", 0.1f),
                getBossEntrySingle("RoR2/DLC1/VoidRaidCrab/cscMiniVoidRaidCrabPhase3.asset", 0.075f),
            };
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return areAnyAvailable(_bossSpawnEntries);
        }

        public override void OnStart()
        {
            DirectorPlacementRule placementRule = SpawnUtils.GetPlacementRule_AtRandomPlayerApproximate(RNG, 30f, 50f);

            DirectorSpawnRequest spawnRequest = new DirectorSpawnRequest(getItemToSpawn(_bossSpawnEntries, RNG), placementRule, RNG);
            spawnRequest.teamIndexOverride = TeamIndex.Monster;

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
                if (!result.success || !bossCombatSquad)
                    return;

                bossCombatSquad.AddMember(result.spawnedInstance.GetComponent<CharacterMaster>());
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
