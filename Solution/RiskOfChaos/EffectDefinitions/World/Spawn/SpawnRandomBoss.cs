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
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RoR2;
using RoR2.ContentManagement;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosEffect("spawn_random_boss", DefaultSelectionWeight = 0.8f)]
    public sealed class SpawnRandomBoss : NetworkBehaviour
    {
        static readonly SpawnPool<CharacterSpawnCard> _spawnPool = new SpawnPool<CharacterSpawnCard>();

        [SystemInitializer(typeof(CharacterExpansionRequirementFix), typeof(ExpansionUtils))]
        static void Init()
        {
            _spawnPool.EnsureCapacity(25);

            _spawnPool.AddAssetEntry(AddressableGuids.RoR2_Base_BeetleQueen_cscBeetleQueen_asset, new SpawnPoolEntryParameters(1f));
            _spawnPool.AddAssetEntry(AddressableGuids.RoR2_Base_Brother_cscBrother_asset, new SpawnPoolEntryParameters(0.7f));
            _spawnPool.AddAssetEntry(AddressableGuids.RoR2_Base_Brother_cscBrotherHurt_asset, new SpawnPoolEntryParameters(0.5f));
            _spawnPool.AddAssetEntry(AddressableGuids.RoR2_Base_ClayBoss_cscClayBoss_asset, new SpawnPoolEntryParameters(1f));
            _spawnPool.AddAssetEntry(AddressableGuids.RoR2_Base_ElectricWorm_cscElectricWorm_asset, new SpawnPoolEntryParameters(0.75f));
            _spawnPool.AddAssetEntry(AddressableGuids.RoR2_Base_Grandparent_cscGrandparent_asset, new SpawnPoolEntryParameters(1f));
            _spawnPool.AddAssetEntry(AddressableGuids.RoR2_Base_Gravekeeper_cscGravekeeper_asset, new SpawnPoolEntryParameters(1f));
            _spawnPool.AddAssetEntry(AddressableGuids.RoR2_Base_ImpBoss_cscImpBoss_asset, new SpawnPoolEntryParameters(1f));
            _spawnPool.AddAssetEntry(AddressableGuids.RoR2_Base_MagmaWorm_cscMagmaWorm_asset, new SpawnPoolEntryParameters(0.85f));
            _spawnPool.AddAssetEntry(AddressableGuids.RoR2_Base_RoboBallBoss_cscRoboBallBoss_asset, new SpawnPoolEntryParameters(1f));
            _spawnPool.AddAssetEntry(AddressableGuids.RoR2_Base_RoboBallBoss_cscSuperRoboBallBoss_asset, new SpawnPoolEntryParameters(1f));
            _spawnPool.AddAssetEntry(AddressableGuids.RoR2_Base_Scav_cscScavBoss_asset, new SpawnPoolEntryParameters(0.9f));
            _spawnPool.AddAssetEntry(AddressableGuids.RoR2_Base_ScavLunar_cscScavLunar_asset, new SpawnPoolEntryParameters(0.7f));
            _spawnPool.AddAssetEntry(AddressableGuids.RoR2_Base_Titan_cscTitanBlackBeach_asset, new SpawnPoolEntryParameters(1f));
            _spawnPool.AddAssetEntry(AddressableGuids.RoR2_Base_Titan_cscTitanGold_asset, new SpawnPoolEntryParameters(0.9f));
            _spawnPool.AddAssetEntry(AddressableGuids.RoR2_Base_Vagrant_cscVagrant_asset, new SpawnPoolEntryParameters(1f));
            _spawnPool.AddAssetEntry(AddressableGuids.RoR2_Junk_BrotherGlass_cscBrotherGlass_asset, new SpawnPoolEntryParameters(0.8f));
            _spawnPool.AddAssetEntry(AddressableGuids.RoR2_DLC1_MajorAndMinorConstruct_cscMegaConstruct_asset, new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC1));

            _spawnPool.AddGroupedEntries([
                _spawnPool.LoadEntry(AddressableGuids.RoR2_DLC1_VoidRaidCrab_cscMiniVoidRaidCrabPhase1_asset, new SpawnPoolEntryParameters(1f, ExpansionUtils.DLC1)),
                _spawnPool.LoadEntry(AddressableGuids.RoR2_DLC1_VoidRaidCrab_cscMiniVoidRaidCrabPhase2_asset, new SpawnPoolEntryParameters(0.9f, ExpansionUtils.DLC1)),
                _spawnPool.LoadEntry(AddressableGuids.RoR2_DLC1_VoidRaidCrab_cscMiniVoidRaidCrabPhase3_asset, new SpawnPoolEntryParameters(0.75f, ExpansionUtils.DLC1)),
            ], 0.85f);

            _spawnPool.AddAssetEntry(AddressableGuids.RoR2_DLC1_VoidMegaCrab_cscVoidMegaCrab_asset, new SpawnPoolEntryParameters(0.6f, ExpansionUtils.DLC1));

            _spawnPool.AddAssetEntry(AddressableGuids.RoR2_DLC3_VultureHunter_cscVultureHunter_asset, new SpawnPoolEntryParameters(0.8f, ExpansionUtils.DLC3));
            _spawnPool.AddAssetEntry(AddressableGuids.RoR2_DLC3_SolusAmalgamator_cscSolusAmalgamator_asset, new SpawnPoolEntryParameters(0.8f, ExpansionUtils.DLC3));

            _spawnPool.TrimExcess();
        }

        [EffectConfig]
        static readonly ConfigHolder<float> _eliteChance =
            ConfigFactory<float>.CreateConfig("Elite Chance", 0.15f)
                                .Description("The likelyhood for the spawned boss to be an elite")
                                .AcceptableValues(new AcceptableValueRange<float>(0f, 1f))
                                .OptionConfig(new StepSliderConfig
                                {
                                    FormatString = "{0:0.##%}",
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

        AssetOrDirectReference<CharacterSpawnCard> _bossSpawnCardRef;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        void OnDestroy()
        {
            _bossSpawnCardRef?.Reset();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            _rng = new Xoroshiro128Plus(_effectComponent.Rng.nextUlong);

            _bossSpawnCardRef = _spawnPool.PickRandomEntry(_rng);
            _bossSpawnCardRef.CallOnLoaded(onSpawnCardLoaded);
        }

        [Server]
        void onSpawnCardLoaded(CharacterSpawnCard spawnCard)
        {
            DirectorPlacementRule placementRule = SpawnUtils.GetPlacementRule_AtRandomPlayerNearestNode(_rng, 30f, float.PositiveInfinity);

            DirectorSpawnRequest spawnRequest = new DirectorSpawnRequest(spawnCard, placementRule, _rng)
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
