using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System.Linq;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.World.Spawn.SpawnCharacter
{
    [ChaosEffect("spawn_random_ally", DefaultSelectionWeight = 0.9f)]
    public sealed class SpawnRandomAlly : GenericSpawnCombatCharacterEffect
    {
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

        [EffectConfig]
        static readonly ConfigHolder<float> _eliteChance =
            ConfigFactory<float>.CreateConfig("Elite Chance", 0.4f)
                                .Description("The likelyhood for the spawned ally to be an elite")
                                .OptionConfig(new StepSliderConfig
                                {
                                    formatString = "{0:P0}",
                                    min = 0f,
                                    max = 1f,
                                    increment = 0.01f
                                })
                                .ValueConstrictor(ValueConstrictors.Clamped01Float)
                                .Build();

        [EffectConfig]
        static readonly ConfigHolder<bool> _allowDirectorUnavailableElites =
            ConfigFactory<bool>.CreateConfig("Ignore Elite Selection Rules", true)
                               .Description("If the effect should ignore normal elite selection rules. If enabled, any elite type can be selected, if disabled, only the elite types that can currently be spawned on the stage can be selected")
                               .OptionConfig(new CheckBoxConfig())
                               .Build();

        protected override float eliteChance => _eliteChance.Value;

        protected override bool allowDirectorUnavailableElites => _allowDirectorUnavailableElites.Value;

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
