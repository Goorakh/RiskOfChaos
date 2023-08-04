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
    [ChaosEffect("spawn_random_enemy")]
    public sealed class SpawnRandomEnemy : GenericSpawnCombatCharacterEffect
    {
        [InitEffectInfo]
        static readonly ChaosEffectInfo _effectInfo;

        static CharacterSpawnEntry[] _spawnEntries;

        [SystemInitializer(typeof(MasterCatalog))]
        static void Init()
        {
            _spawnEntries = getAllValidMasterPrefabs(false).Where(master =>
            {
                CharacterBody bodyPrefab = master.bodyPrefab.GetComponent<CharacterBody>();

                // Exclude bosses from being spawned, there is already an effect for that after all
                if (bodyPrefab.isChampion)
                {
#if DEBUG
                    Log.Debug($"Excluding master {master}: boss");
#endif
                    return false;
                }

                return true;
            }).Select(master =>
            {
                return new CharacterSpawnEntry(master, 1f);
            }).ToArray();
        }

        static ConfigEntry<float> _eliteChanceConfig;
        const float ELITE_CHANCE_CONFIG_DEFAULT_VALUE = 0.3f;

        protected override float eliteChance => _eliteChanceConfig?.Value ?? ELITE_CHANCE_CONFIG_DEFAULT_VALUE;

        static ConfigEntry<bool> _allowDirectorUnavailableElitesConfig;
        const bool ALLOW_DIRECTOR_UNAVAILABLE_ELITES_CONFIG_DEFAULT_VALUE = true;

        protected override bool allowDirectorUnavailableElites => _allowDirectorUnavailableElitesConfig?.Value ?? ALLOW_DIRECTOR_UNAVAILABLE_ELITES_CONFIG_DEFAULT_VALUE;

        [SystemInitializer(typeof(ChaosEffectCatalog))]
        static void InitConfigs()
        {
            _eliteChanceConfig = _effectInfo.BindConfig("Elite Chance", ELITE_CHANCE_CONFIG_DEFAULT_VALUE, new ConfigDescription("The likelyhood for the spawned enemy to be an elite"));

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
            CharacterMaster enemySpawnPrefab = getItemToSpawn(_spawnEntries, RNG);
            setupPrefab(enemySpawnPrefab);

            foreach (CharacterBody playerBody in PlayerUtils.GetAllPlayerBodies(true))
            {
                new MasterSummon()
                {
                    masterPrefab = enemySpawnPrefab.gameObject,
                    position = getProperSpawnPosition(playerBody.footPosition, enemySpawnPrefab, RNG),
                    rotation = Quaternion.identity,
                    teamIndexOverride = TeamIndex.Monster,
                    ignoreTeamMemberLimit = true,
                    useAmbientLevel = true,
                    preSpawnSetupCallback = onSpawned,
                }.Perform();
            }
        }

        protected override void onSpawned(CharacterMaster master)
        {
            base.onSpawned(master);

            master.gameObject.SetDontDestroyOnLoad(false);
        }
    }
}
