using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
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
            _spawnEntries = getAllValidMasterPrefabs().Select(master =>
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

        [EffectCanActivate]
        static bool CanActivate()
        {
            return areAnyAvailable(_spawnEntries);
        }

        public override void OnStart()
        {
            CharacterMaster enemySpawnPrefab = getItemToSpawn(_spawnEntries, RNG);

            foreach (CharacterBody playerBody in PlayerUtils.GetAllPlayerBodies(true))
            {
                new MasterSummon()
                {
                    summonerBodyObject = playerBody.gameObject,
                    masterPrefab = enemySpawnPrefab.gameObject,
                    position = getProperSpawnPosition(playerBody.footPosition, enemySpawnPrefab, RNG),
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

            if (!master.GetComponent<SetDontDestroyOnLoad>())
            {
                master.gameObject.AddComponent<SetDontDestroyOnLoad>();
            }
        }
    }
}
