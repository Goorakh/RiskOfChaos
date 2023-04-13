using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosEffect("spawn_random_boss", DefaultSelectionWeight = 0.8f)]
    public sealed class SpawnRandomBoss : BaseEffect
    {
        readonly struct BossSelection
        {
            public readonly float Weight;
            readonly CharacterSpawnCard[] _spawnCards;

            public BossSelection(CharacterSpawnCard[] spawnCards, float weight)
            {
                _spawnCards = spawnCards;
                Weight = weight;
            }

            public BossSelection(CharacterSpawnCard spawnCard, float weight) : this(new CharacterSpawnCard[] { spawnCard }, weight)
            {
            }

            public readonly CharacterSpawnCard GetSpawnCard(Xoroshiro128Plus rng)
            {
                return rng.NextElementUniform(_spawnCards.Where(canSelectBoss).ToList());
            }

            public readonly bool CanBeSelected()
            {
                return _spawnCards.Any(canSelectBoss);
            }

            static bool canSelectBossPrefab(GameObject prefab)
            {
                return prefab && ExpansionUtils.IsCharacterMasterExpansionAvailable(prefab);
            }

            static bool canSelectBoss(CharacterSpawnCard card)
            {
                if (!card)
                    return false;

                if (card is MultiCharacterSpawnCard multiCard)
                {
                    return multiCard.masterPrefabs.All(canSelectBossPrefab);
                }
                else
                {
                    return canSelectBossPrefab(card.prefab);
                }
            }
        }

        static readonly List<BossSelection> _allBossSelections = new List<BossSelection>();

        static readonly GameObject _bossCombatSquadPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Core/BossCombatSquad.prefab").WaitForCompletion();

        [SystemInitializer]
        static void Init()
        {
            static void loadBossPrefab(string assetPath, float weight)
            {
                _allBossSelections.Add(new BossSelection(Addressables.LoadAssetAsync<CharacterSpawnCard>(assetPath).WaitForCompletion(), weight));
            }

            static void loadBossPrefabs(string[] assetPaths, float weight)
            {
                CharacterSpawnCard[] spawnCards = Array.ConvertAll(assetPaths, path => Addressables.LoadAssetAsync<CharacterSpawnCard>(path).WaitForCompletion());
                _allBossSelections.Add(new BossSelection(spawnCards, weight));
            }

            loadBossPrefab("RoR2/Base/Beetle/cscBeetleQueen.asset", 1f);
            loadBossPrefab("RoR2/Base/Brother/cscBrother.asset", 0.5f);
            loadBossPrefab("RoR2/Base/Brother/cscBrotherHurt.asset", 0.4f);
            loadBossPrefab("RoR2/Base/ClayBoss/cscClayBoss.asset", 1f);
            loadBossPrefab("RoR2/Base/ElectricWorm/cscElectricWorm.asset", 0.75f);
            loadBossPrefab("RoR2/Base/Grandparent/cscGrandparent.asset", 1f);
            loadBossPrefab("RoR2/Base/Gravekeeper/cscGravekeeper.asset", 1f);
            loadBossPrefab("RoR2/Base/ImpBoss/cscImpBoss.asset", 1f);
            loadBossPrefab("RoR2/Base/MagmaWorm/cscMagmaWorm.asset", 0.85f);
            loadBossPrefab("RoR2/Base/RoboBallBoss/cscRoboBallBoss.asset", 1f);
            loadBossPrefab("RoR2/Base/RoboBallBoss/cscSuperRoboBallBoss.asset", 1f);
            loadBossPrefab("RoR2/Base/Scav/cscScavBoss.asset", 0.9f);
            loadBossPrefab("RoR2/Base/ScavLunar/cscScavLunar.asset", 0.8f);

            loadBossPrefabs(new string[]
            {
                "RoR2/Base/Titan/cscTitanBlackBeach.asset",
                "RoR2/Base/Titan/cscTitanDampCave.asset",
                "RoR2/Base/Titan/cscTitanGolemPlains.asset",
                "RoR2/Base/Titan/cscTitanGooLake.asset"
            }, 1f);

            loadBossPrefab("RoR2/Base/Titan/cscTitanGold.asset", 0.9f);
            loadBossPrefab("RoR2/Base/Vagrant/cscVagrant.asset", 1f);
            loadBossPrefab("RoR2/Junk/BrotherGlass/cscBrotherGlass.asset", 0.7f);
            loadBossPrefab("RoR2/DLC1/MajorAndMinorConstruct/cscMegaConstruct.asset", 1f);
            loadBossPrefab("RoR2/DLC1/VoidRaidCrab/cscMiniVoidRaidCrabPhase1.asset", 0.1f);
            loadBossPrefab("RoR2/DLC1/VoidRaidCrab/cscMiniVoidRaidCrabPhase2.asset", 0.1f);
            loadBossPrefab("RoR2/DLC1/VoidRaidCrab/cscMiniVoidRaidCrabPhase3.asset", 0.075f);
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return DirectorCore.instance && _allBossSelections.Any(b => b.CanBeSelected());
        }

        static WeightedSelection<BossSelection> getWeightedBossSelection()
        {
            WeightedSelection<BossSelection> selector = new WeightedSelection<BossSelection>(_allBossSelections.Count);
            foreach (BossSelection bossSelection in _allBossSelections)
            {
                if (bossSelection.CanBeSelected())
                {
                    selector.AddChoice(bossSelection, bossSelection.Weight);
                }
            }

            return selector;
        }

        public override void OnStart()
        {
            BossSelection bossCardSelection = getWeightedBossSelection().Evaluate(RNG.nextNormalizedFloat);

            DirectorPlacementRule placementRule = SpawnUtils.GetPlacementRule_AtRandomPlayerApproximate(RNG, 30f, 50f);

            DirectorSpawnRequest spawnRequest = new DirectorSpawnRequest(bossCardSelection.GetSpawnCard(RNG), placementRule, RNG);
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
