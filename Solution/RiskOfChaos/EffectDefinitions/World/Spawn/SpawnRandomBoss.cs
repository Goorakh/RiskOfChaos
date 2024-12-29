using BepInEx.Configuration;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.EffectUtils.World.Spawn;
using RiskOfChaos.Patches;
using RiskOfChaos.Utilities;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosEffect("spawn_random_boss", DefaultSelectionWeight = 0.8f)]
    public sealed class SpawnRandomBoss : NetworkBehaviour
    {
        static readonly SpawnPool<CharacterSpawnCard> _spawnPool = new SpawnPool<CharacterSpawnCard>
        {
            RequiredExpansionsProvider = SpawnPoolUtils.CharacterSpawnCardExpansionsProvider
        };

        [SystemInitializer(typeof(CharacterExpansionRequirementFix))]
        static void Init()
        {
            _spawnPool.EnsureCapacity(25);

            _spawnPool.AddAssetEntry("RoR2/Base/Beetle/cscBeetleQueen.asset", new SpawnPoolEntryParameters(1f));
            _spawnPool.AddAssetEntry("RoR2/Base/Brother/cscBrother.asset", new SpawnPoolEntryParameters(0.7f));
            _spawnPool.AddAssetEntry("RoR2/Base/Brother/cscBrotherHurt.asset", new SpawnPoolEntryParameters(0.5f));
            _spawnPool.AddAssetEntry("RoR2/Base/ClayBoss/cscClayBoss.asset", new SpawnPoolEntryParameters(1f));
            _spawnPool.AddAssetEntry("RoR2/Base/ElectricWorm/cscElectricWorm.asset", new SpawnPoolEntryParameters(0.75f));
            _spawnPool.AddAssetEntry("RoR2/Base/Grandparent/cscGrandparent.asset", new SpawnPoolEntryParameters(1f));
            _spawnPool.AddAssetEntry("RoR2/Base/Gravekeeper/cscGravekeeper.asset", new SpawnPoolEntryParameters(1f));
            _spawnPool.AddAssetEntry("RoR2/Base/ImpBoss/cscImpBoss.asset", new SpawnPoolEntryParameters(1f));
            _spawnPool.AddAssetEntry("RoR2/Base/MagmaWorm/cscMagmaWorm.asset", new SpawnPoolEntryParameters(0.85f));
            _spawnPool.AddAssetEntry("RoR2/Base/RoboBallBoss/cscRoboBallBoss.asset", new SpawnPoolEntryParameters(1f));
            _spawnPool.AddAssetEntry("RoR2/Base/RoboBallBoss/cscSuperRoboBallBoss.asset", new SpawnPoolEntryParameters(1f));
            _spawnPool.AddAssetEntry("RoR2/Base/Scav/cscScavBoss.asset", new SpawnPoolEntryParameters(0.9f));
            _spawnPool.AddAssetEntry("RoR2/Base/ScavLunar/cscScavLunar.asset", new SpawnPoolEntryParameters(0.7f));
            _spawnPool.AddAssetEntry("RoR2/Base/Titan/cscTitanBlackBeach.asset", new SpawnPoolEntryParameters(1f));
            _spawnPool.AddAssetEntry("RoR2/Base/Titan/cscTitanGold.asset", new SpawnPoolEntryParameters(0.9f));
            _spawnPool.AddAssetEntry("RoR2/Base/Vagrant/cscVagrant.asset", new SpawnPoolEntryParameters(1f));
            _spawnPool.AddAssetEntry("RoR2/Junk/BrotherGlass/cscBrotherGlass.asset", new SpawnPoolEntryParameters(0.8f));
            _spawnPool.AddAssetEntry("RoR2/DLC1/MajorAndMinorConstruct/cscMegaConstruct.asset", new SpawnPoolEntryParameters(1f));

            _spawnPool.AddGroupedEntries([
                _spawnPool.LoadEntry("RoR2/DLC1/VoidRaidCrab/cscMiniVoidRaidCrabPhase1.asset", new SpawnPoolEntryParameters(1f)),
                _spawnPool.LoadEntry("RoR2/DLC1/VoidRaidCrab/cscMiniVoidRaidCrabPhase2.asset", new SpawnPoolEntryParameters(0.9f)),
                _spawnPool.LoadEntry("RoR2/DLC1/VoidRaidCrab/cscMiniVoidRaidCrabPhase3.asset", new SpawnPoolEntryParameters(0.75f)),
            ], 0.85f);

            _spawnPool.AddAssetEntry("RoR2/DLC1/VoidMegaCrab/cscVoidMegaCrab.asset", new SpawnPoolEntryParameters(0.6f));

            _spawnPool.TrimExcess();
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

            spawnRequest.onSpawnedServer = result =>
            {
                if (!result.success)
                    return;

                if (result.spawnedInstance.TryGetComponent(out CharacterMaster master))
                {
                    CombatCharacterSpawnHelper.SetupSpawnedCombatCharacter(master, _rng);

                    if (_rng.nextNormalizedFloat <= _eliteChance.Value)
                    {
                        CombatCharacterSpawnHelper.GrantRandomEliteAspect(master, _rng, _allowDirectorUnavailableElites.Value, true);
                    }

                    spawnedMasters.Add(master);
                }
            };

            spawnRequest.SpawnWithFallbackPlacement(SpawnUtils.GetBestValidRandomPlacementRule());

            if (spawnedMasters.Count > 0)
            {
                GameObject bossCombatSquadObj = Instantiate(RoCContent.NetworkedPrefabs.BossCombatSquadNoReward);

                CombatSquad bossCombatSquad = bossCombatSquadObj.GetComponent<CombatSquad>();
                foreach (CharacterMaster master in spawnedMasters)
                {
                    bossCombatSquad.AddMember(master);
                }

                NetworkServer.Spawn(bossCombatSquadObj);
            }
        }
    }
}
