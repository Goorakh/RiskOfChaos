using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Utilities;
using RoR2;
using System;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.World.Spawn.SpawnCharacter
{
    [ChaosEffect("spawn_random_ally")]
    public sealed class SpawnRandomAlly : GenericSpawnCombatCharacterEffect
    {
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
