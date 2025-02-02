using BepInEx.Configuration;
using RiskOfChaos.ChatMessages;
using RiskOfChaos.Collections.ParsedValue;
using RiskOfChaos.ConfigHandling;
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
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Spawn.SpawnCharacter
{
    [ChaosEffect("spawn_random_enemy")]
    public sealed class SpawnRandomEnemy : NetworkBehaviour
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

            _spawnPool.CalcIsEntryAvailable += entry =>
            {
                CharacterBody bodyPrefab = null;
                if (entry && entry.prefab && entry.prefab.TryGetComponent(out CharacterMaster masterPrefab) && masterPrefab.bodyPrefab)
                {
                    bodyPrefab = masterPrefab.bodyPrefab.GetComponent<CharacterBody>();
                }

                return bodyPrefab && !_enemyBlacklist.Contains(bodyPrefab.bodyIndex);
            };

            foreach (CharacterMaster master in validCombatCharacters)
            {
                CharacterBody bodyPrefab = master.bodyPrefab.GetComponent<CharacterBody>();
                if (bodyPrefab.isChampion)
                    continue;

                float weight = 1f;

                SurvivorIndex survivorIndex = SurvivorCatalog.GetSurvivorIndexFromBodyIndex(bodyPrefab.bodyIndex);
                if (survivorIndex != SurvivorIndex.None)
                {
                    weight *= 0.75f;
                }

                if ((bodyPrefab.bodyFlags & CharacterBody.BodyFlags.Mechanical) != 0)
                {
                    weight *= 0.6f;
                }

                CharacterSpawnCard spawnCard = ScriptableObject.CreateInstance<CharacterSpawnCard>();
                spawnCard.name = $"cscEnemy{master.name}";
                spawnCard.prefab = master.gameObject;
                spawnCard.sendOverNetwork = true;
                spawnCard.hullSize = bodyPrefab.hullClassification;
                spawnCard.nodeGraphType = CombatCharacterSpawnHelper.GetSpawnGraphType(master);
                spawnCard.requiredFlags = NodeFlags.None;
                spawnCard.forbiddenFlags = NodeFlags.NoCharacterSpawn;

                _spawnPool.AddEntry(spawnCard, new SpawnPoolEntryParameters(weight));
            }

            _spawnPool.TrimExcess();
        }

        [EffectConfig]
        static readonly ConfigHolder<float> _eliteChance =
            ConfigFactory<float>.CreateConfig("Elite Chance", 0.3f)
                                .Description("The likelyhood for the spawned enemy to be an elite")
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

        [EffectConfig]
        static readonly ConfigHolder<string> _enemyBlacklistConfig =
            ConfigFactory<string>.CreateConfig("Enemy Blacklist", string.Empty)
                                 .Description("A comma-separated list of characters to exclude from the enemy pool. Internal body names and English display names are allowed, with spaces and commas removed.")
                                 .OptionConfig(new InputFieldConfig())
                                 .Build();

        static readonly ParsedBodyList _enemyBlacklist = new ParsedBodyList
        {
            ConfigHolder = _enemyBlacklistConfig
        };

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _spawnPool.AnyAvailable;
        }

        ChaosEffectComponent _effectComponent;

        Xoroshiro128Plus _rng;

        CharacterSpawnCard _enemySpawnCard;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            _rng = new Xoroshiro128Plus(_effectComponent.Rng.nextUlong);

            _enemySpawnCard = _spawnPool.PickRandomEntry(_rng);
        }

        void Start()
        {
            if (!NetworkServer.active)
                return;

            List<CharacterMaster> spawnedMasters = new List<CharacterMaster>(PlayerCharacterMasterController.instances.Count);

            foreach (PlayerCharacterMasterController playerMaster in PlayerCharacterMasterController.instances)
            {
                if (!playerMaster.isConnected)
                    continue;

                CharacterMaster master = playerMaster.master;
                if (!master || master.IsDeadAndOutOfLivesServer())
                    continue;

                if (!master.TryGetBodyPosition(out Vector3 bodyPosition))
                    continue;

                Xoroshiro128Plus spawnRng = new Xoroshiro128Plus(_rng.nextUlong);

                DirectorPlacementRule placementRule = new DirectorPlacementRule
                {
                    placementMode = DirectorPlacementRule.PlacementMode.Approximate,
                    position = bodyPosition,
                    preventOverhead = true,
                    minDistance = 10f,
                    maxDistance = 50f
                };

                DirectorSpawnRequest spawnRequest = new DirectorSpawnRequest(_enemySpawnCard, placementRule, spawnRng)
                {
                    ignoreTeamMemberLimit = true,
                    teamIndexOverride = TeamIndex.Monster,
                    onSpawnedServer = onEnemySpawnedServer
                };

                void onEnemySpawnedServer(SpawnCard.SpawnResult result)
                {
                    if (!result.success)
                        return;

                    if (result.spawnedInstance.TryGetComponent(out CharacterMaster master))
                    {
                        CombatCharacterSpawnHelper.SetupSpawnedCombatCharacter(master, spawnRng);

                        if (spawnRng.nextNormalizedFloat <= _eliteChance.Value)
                        {
                            CombatCharacterSpawnHelper.GrantRandomEliteAspect(master, spawnRng, _allowDirectorUnavailableElites.Value);
                        }

                        master.gameObject.SetDontDestroyOnLoad(false);

                        spawnedMasters.Add(master);
                    }
                }

                spawnRequest.SpawnWithFallbackPlacement(SpawnUtils.GetBestValidRandomPlacementRule());
            }

            if (spawnedMasters.Count > 0)
            {
                RoR2Application.onNextUpdate += () =>
                {
                    CharacterBody spawnedBody = null;
                    foreach (CharacterMaster master in spawnedMasters)
                    {
                        CharacterBody body = master.GetBody();
                        if (body)
                        {
                            spawnedBody = body;
                            break;
                        }
                    }

                    Chat.SendBroadcastChat(new BestNameSubjectChatMessage
                    {
                        BaseToken = "RANDOM_ENEMY_SPAWN_MESSAGE",
                        SubjectAsCharacterBody = spawnedBody,
                        SubjectNameOverrideColor = Color.white,
                    });
                };   
            }
        }
    }
}
