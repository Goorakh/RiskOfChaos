using BepInEx.Configuration;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using RoR2;
using System.Linq;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.World.Spawn.SpawnCharacter
{
    [ChaosEffect("spawn_random_ally", DefaultSelectionWeight = 0.9f)]
    public sealed class SpawnRandomAlly : GenericSpawnCombatCharacterEffect
    {
        [InitEffectInfo]
        static readonly ChaosEffectInfo _effectInfo;

        static CharacterSpawnEntry[] _spawnEntries;

        [SystemInitializer(typeof(MasterCatalog))]
        static void Init()
        {
            _spawnEntries = getAllValidMasterPrefabs(true).Select(master =>
            {
                CharacterBody bodyPrefab = master.bodyPrefab.GetComponent<CharacterBody>();

                float weight;
                if (bodyPrefab.isChampion)
                {
                    weight = 0.5f;
                }
                else
                {
                    weight = 1f;
                }

                return new CharacterSpawnEntry(master, weight);
            }).ToArray();
        }

        static ConfigEntry<float> _eliteChanceConfig;
        const float ELITE_CHANCE_CONFIG_DEFAULT_VALUE = 0.4f;

        protected override float eliteChance
        {
            get
            {
                if (_eliteChanceConfig == null)
                {
                    return ELITE_CHANCE_CONFIG_DEFAULT_VALUE;
                }
                else
                {
                    return Mathf.Clamp01(_eliteChanceConfig.Value);
                }
            }
        }

        static ConfigEntry<bool> _allowDirectorUnavailableElitesConfig;
        const bool ALLOW_DIRECTOR_UNAVAILABLE_ELITES_CONFIG_DEFAULT_VALUE = true;

        protected override bool allowDirectorUnavailableElites => _allowDirectorUnavailableElitesConfig?.Value ?? ALLOW_DIRECTOR_UNAVAILABLE_ELITES_CONFIG_DEFAULT_VALUE;

        [SystemInitializer(typeof(ChaosEffectCatalog))]
        static void InitConfigs()
        {
            _eliteChanceConfig = _effectInfo.BindConfig("Elite Chance", ELITE_CHANCE_CONFIG_DEFAULT_VALUE, new ConfigDescription("The likelyhood for the spawned ally to be an elite"));

            addConfigOption(new StepSliderOption(_eliteChanceConfig, new StepSliderConfig
            {
                formatString = "{0:P0}",
                min = 0f,
                max = 1f,
                increment = 0.01f
            }));

            _allowDirectorUnavailableElitesConfig = _effectInfo.BindConfig("Ignore Elite Selection Rules", ALLOW_DIRECTOR_UNAVAILABLE_ELITES_CONFIG_DEFAULT_VALUE, new ConfigDescription("If the effect should ignore normal elite selection rules. If enabled, any elite type can be selected, if disabled, only the elite types that can currently be spawned on the stage can be selected"));

            addConfigOption(new CheckBoxOption(_allowDirectorUnavailableElitesConfig));
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return areAnyAvailable(_spawnEntries);
        }

        public override void OnStart()
        {
            CharacterMaster allySpawnPrefab = getItemToSpawn(_spawnEntries, RNG);
            setupPrefab(allySpawnPrefab);

            foreach (CharacterBody playerBody in PlayerUtils.GetAllPlayerBodies(true))
            {
                new MasterSummon()
                {
                    summonerBodyObject = playerBody.gameObject,
                    masterPrefab = allySpawnPrefab.gameObject,
                    position = getProperSpawnPosition(playerBody.footPosition, allySpawnPrefab, RNG),
                    rotation = Quaternion.identity,
                    ignoreTeamMemberLimit = true,
                    useAmbientLevel = true,
                    preSpawnSetupCallback = onSpawned
                }.Perform();
            }
        }

        protected override void onSpawned(CharacterMaster master)
        {
            base.onSpawned(master);

            master.gameObject.SetDontDestroyOnLoad(true);
        }
    }
}
