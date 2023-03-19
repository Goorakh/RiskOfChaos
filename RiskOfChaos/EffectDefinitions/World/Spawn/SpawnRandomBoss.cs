using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RoR2;
using RoR2.ExpansionManagement;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosEffect("spawn_random_boss", DefaultSelectionWeight = 0.8f)]
    public sealed class SpawnRandomBoss : BaseEffect
    {
        static readonly WeightedSelection<CharacterSpawnCard> _bossSelector;

        static readonly GameObject _bossCombatSquadPrefab;

        static SpawnRandomBoss()
        {
            _bossCombatSquadPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Core/BossCombatSquad.prefab").WaitForCompletion();

            _bossSelector = new WeightedSelection<CharacterSpawnCard>();
            _bossSelector.AddChoice(Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/Base/Beetle/cscBeetleQueen.asset").WaitForCompletion(), 1f);
            _bossSelector.AddChoice(Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/Base/Brother/cscBrother.asset").WaitForCompletion(), 0.5f);
            _bossSelector.AddChoice(Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/Base/Brother/cscBrotherHurt.asset").WaitForCompletion(), 0.4f);
            _bossSelector.AddChoice(Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/Base/ClayBoss/cscClayBoss.asset").WaitForCompletion(), 1f);
            _bossSelector.AddChoice(Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/Base/ElectricWorm/cscElectricWorm.asset").WaitForCompletion(), 0.75f);
            _bossSelector.AddChoice(Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/Base/Grandparent/cscGrandparent.asset").WaitForCompletion(), 1f);
            _bossSelector.AddChoice(Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/Base/Gravekeeper/cscGravekeeper.asset").WaitForCompletion(), 1f);
            _bossSelector.AddChoice(Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/Base/ImpBoss/cscImpBoss.asset").WaitForCompletion(), 1f);
            _bossSelector.AddChoice(Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/Base/MagmaWorm/cscMagmaWorm.asset").WaitForCompletion(), 0.85f);
            _bossSelector.AddChoice(Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/Base/RoboBallBoss/cscRoboBallBoss.asset").WaitForCompletion(), 1f);
            _bossSelector.AddChoice(Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/Base/RoboBallBoss/cscSuperRoboBallBoss.asset").WaitForCompletion(), 1f);
            _bossSelector.AddChoice(Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/Base/Scav/cscScavBoss.asset").WaitForCompletion(), 0.9f);
            _bossSelector.AddChoice(Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/Base/ScavLunar/cscScavLunar.asset").WaitForCompletion(), 0.8f);

            const float TITAN_WEIGHT_TOTAL = 1.5f;
            const int TITAN_CARD_COUNT = 4;
            const float TITAN_CARD_WEIGHT = TITAN_WEIGHT_TOTAL / TITAN_CARD_COUNT;

            _bossSelector.AddChoice(Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/Base/Titan/cscTitanBlackBeach.asset").WaitForCompletion(), TITAN_CARD_WEIGHT);
            _bossSelector.AddChoice(Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/Base/Titan/cscTitanDampCave.asset").WaitForCompletion(), TITAN_CARD_WEIGHT);
            _bossSelector.AddChoice(Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/Base/Titan/cscTitanGolemPlains.asset").WaitForCompletion(), TITAN_CARD_WEIGHT);
            _bossSelector.AddChoice(Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/Base/Titan/cscTitanGooLake.asset").WaitForCompletion(), TITAN_CARD_WEIGHT);

            _bossSelector.AddChoice(Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/Base/Titan/cscTitanGold.asset").WaitForCompletion(), 0.9f);
            _bossSelector.AddChoice(Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/Base/Vagrant/cscVagrant.asset").WaitForCompletion(), 1f);

            _bossSelector.AddChoice(Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/Junk/BrotherGlass/cscBrotherGlass.asset").WaitForCompletion(), 0.7f);

            _bossSelector.AddChoice(Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/DLC1/MajorAndMinorConstruct/cscMegaConstruct.asset").WaitForCompletion(), 1f);

            _bossSelector.AddChoice(Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/DLC1/VoidRaidCrab/cscMiniVoidRaidCrabPhase1.asset").WaitForCompletion(), 0.1f);
            _bossSelector.AddChoice(Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/DLC1/VoidRaidCrab/cscMiniVoidRaidCrabPhase2.asset").WaitForCompletion(), 0.1f);
            _bossSelector.AddChoice(Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/DLC1/VoidRaidCrab/cscMiniVoidRaidCrabPhase3.asset").WaitForCompletion(), 0.05f);
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            static bool isAnyBossAvailable()
            {
                for (int i = 0; i < _bossSelector.Count; i++)
                {
                    if (canSelectBoss(_bossSelector.GetChoice(i).value))
                    {
                        return true;
                    }
                }

                return false;
            }

            return DirectorCore.instance && isAnyBossAvailable();
        }

        static bool canSelectBossPrefab(GameObject prefab)
        {
            if (!prefab)
                return false;

            if (prefab.TryGetComponent(out ExpansionRequirementComponent expansionRequirement))
            {
                Run run = Run.instance;
                if (run && expansionRequirement.requiredExpansion && !run.IsExpansionEnabled(expansionRequirement.requiredExpansion))
                {
                    return false;
                }
            }

            return true;
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

        public override void OnStart()
        {
            CharacterSpawnCard bossCard;

            do
            {
                bossCard = _bossSelector.Evaluate(RNG.nextNormalizedFloat);
            } while (!canSelectBoss(bossCard));

            DirectorPlacementRule placementRule = SpawnUtils.GetPlacementRule_AtRandomPlayerApproximate(RNG, 30f, 50f);

            DirectorSpawnRequest spawnRequest = new DirectorSpawnRequest(bossCard, placementRule, RNG);
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
