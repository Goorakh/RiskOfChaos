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
using RoR2.Navigation;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Spawn.SpawnCharacter
{
    [ChaosEffect("spawn_random_ally")]
    public sealed class SpawnRandomAlly : NetworkBehaviour
    {
        static readonly SpawnPool<CharacterSpawnCard> _spawnPool = new SpawnPool<CharacterSpawnCard>
        {
            RequiredExpansionsProvider = SpawnPoolUtils.CharacterSpawnCardExpansionsProvider
        };

        [SystemInitializer(typeof(MasterCatalog), typeof(CharacterExpansionRequirementFix))]
        static void Init()
        {
            List<CharacterMaster> validCombatCharacters = [];
            CombatCharacterSpawnHelper.GetAllValidCombatCharacters(validCombatCharacters);

            _spawnPool.EnsureCapacity(validCombatCharacters.Count);

            for (int i = 0; i < validCombatCharacters.Count; i++)
            {
                CharacterMaster master = validCombatCharacters[i];

                string masterName = master.name;
                if (masterName.EndsWith("Master", StringComparison.OrdinalIgnoreCase))
                {
                    string allyMasterName = masterName.Insert(masterName.Length - 6, "Ally");

                    MasterCatalog.MasterIndex allyMasterIndex = MasterCatalog.FindMasterIndex(allyMasterName);
                    if (allyMasterIndex.isValid)
                    {
                        GameObject allyMasterPrefabObject = MasterCatalog.GetMasterPrefab(allyMasterIndex);
                        CharacterMaster allyMasterPrefab = allyMasterPrefabObject ? allyMasterPrefabObject.GetComponent<CharacterMaster>() : null;

                        if (allyMasterPrefab)
                        {
                            Log.Debug($"Replaced {master.name} with ally variant: {allyMasterPrefab.name}");

                            master = allyMasterPrefab;
                            masterName = allyMasterName;
                        }
                    }
                }

                CharacterBody bodyPrefab = master.bodyPrefab.GetComponent<CharacterBody>();

                float weight = 1f;
                if (bodyPrefab.isChampion)
                {
                    weight *= 0.7f;
                }

                CharacterSpawnCard spawnCard = ScriptableObject.CreateInstance<CharacterSpawnCard>();
                spawnCard.name = $"cscAlly{master.name}";
                spawnCard.prefab = master.gameObject;
                spawnCard.sendOverNetwork = true;
                spawnCard.hullSize = bodyPrefab.hullClassification;
                spawnCard.nodeGraphType = CombatCharacterSpawnHelper.GetSpawnGraphType(master);
                spawnCard.requiredFlags = NodeFlags.None;
                spawnCard.forbiddenFlags = NodeFlags.NoCharacterSpawn;

                spawnCard.itemsToGrant = [
                    new ItemCountPair
                    {
                        itemDef = RoCContent.Items.MinAllyRegen,
                        count = 1
                    }
                ];

                _spawnPool.AddEntry(spawnCard, new SpawnPoolEntryParameters(weight));
            }

            _spawnPool.TrimExcess();
        }

        [EffectConfig]
        static readonly ConfigHolder<float> _eliteChance =
            ConfigFactory<float>.CreateConfig("Elite Chance", 0.4f)
                                .Description("The likelyhood for the spawned ally to be an elite")
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
        CharacterSpawnCard _allySpawnCard;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            _rng = new Xoroshiro128Plus(_effectComponent.Rng.nextUlong);

            _allySpawnCard = _spawnPool.PickRandomEntry(_rng);
        }

        void Start()
        {
            if (!NetworkServer.active)
                return;

            foreach (PlayerCharacterMasterController playerMaster in PlayerCharacterMasterController.instances)
            {
                if (!playerMaster.isConnected)
                    continue;

                CharacterMaster master = playerMaster.master;
                CharacterBody body = master ? master.GetBody() : null;
                if (!body || !master || master.IsDeadAndOutOfLivesServer())
                    continue;

                DirectorPlacementRule placementRule = new DirectorPlacementRule
                {
                    placementMode = DirectorPlacementRule.PlacementMode.NearestNode,
                    position = body.corePosition,
                    preventOverhead = true
                };

                DirectorSpawnRequest spawnRequest = new DirectorSpawnRequest(_allySpawnCard, placementRule, _rng)
                {
                    ignoreTeamMemberLimit = true,
                    summonerBodyObject = body.gameObject,
                    teamIndexOverride = master.teamIndex,
                    onSpawnedServer = onAllySpawnedServer
                };

                spawnRequest.SpawnWithFallbackPlacement(SpawnUtils.GetBestValidRandomPlacementRule());
            }
        }

        void onAllySpawnedServer(SpawnCard.SpawnResult result)
        {
            if (!result.success)
                return;

            if (result.spawnedInstance.TryGetComponent(out CharacterMaster master))
            {
                CombatCharacterSpawnHelper.SetupSpawnedCombatCharacter(master, _rng);
                CombatCharacterSpawnHelper.TryGrantEliteAspect(master, _rng, _eliteChance.Value, _allowDirectorUnavailableElites.Value);

                master.gameObject.SetDontDestroyOnLoad(true);
            }
        }
    }
}
