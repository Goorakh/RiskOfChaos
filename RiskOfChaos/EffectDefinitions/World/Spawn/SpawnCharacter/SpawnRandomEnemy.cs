using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Utilities;
using RoR2;
using RoR2.Navigation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RiskOfChaos.EffectDefinitions.World.Spawn.SpawnCharacter
{
    [ChaosEffect("spawn_random_enemy")]
    public sealed class SpawnRandomEnemy : GenericSpawnCombatCharacterEffect
    {
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

            if (master.TryGetComponent(out SetDontDestroyOnLoad setDontDestroyOnLoad))
            {
                GameObject.Destroy(setDontDestroyOnLoad);
            }

            if (Util.IsDontDestroyOnLoad(master.gameObject))
            {
                SceneManager.MoveGameObjectToScene(master.gameObject, SceneManager.GetActiveScene());
            }
        }
    }
}
