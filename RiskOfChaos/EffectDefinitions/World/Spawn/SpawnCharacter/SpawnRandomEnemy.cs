using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System.Linq;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.World.Spawn.SpawnCharacter
{
    [ChaosEffect("spawn_random_enemy")]
    public sealed class SpawnRandomEnemy : GenericSpawnCombatCharacterEffect
    {
        static CharacterSpawnEntry[] _spawnEntries;

        [SystemInitializer(typeof(MasterCatalog))]
        static void Init()
        {
            _spawnEntries = getAllValidMasterPrefabs().Where(master =>
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
