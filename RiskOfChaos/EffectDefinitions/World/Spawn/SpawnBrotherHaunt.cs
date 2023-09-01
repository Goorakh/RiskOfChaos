using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosTimedEffect("spawn_brother_haunt", 45f, DefaultSelectionWeight = 0.7f)]
    public sealed class SpawnBrotherHaunt : TimedEffect
    {
        static readonly GameObject _brotherHauntPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/BrotherHaunt/BrotherHauntMaster.prefab").WaitForCompletion();

        static readonly MasterSummon _brotherHauntSummon = new MasterSummon
        {
            masterPrefab = _brotherHauntPrefab,
            ignoreTeamMemberLimit = true,
            teamIndexOverride = TeamIndex.Lunar
        };

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _brotherHauntPrefab;
        }

        CharacterMaster _spawnedMaster;

        public override void OnStart()
        {
            RoR2Application.onFixedUpdate += fixedUpdate;
        }

        float _nextRespawnAttemptTime = 0f;
        void fixedUpdate()
        {
            if (_nextRespawnAttemptTime >= TimeElapsed)
            {
                if (!_spawnedMaster || _spawnedMaster.IsDeadAndOutOfLivesServer())
                {
#if DEBUG
                    Log.Debug("Spawned master is null or dead, respawning...");
#endif

                    _spawnedMaster = _brotherHauntSummon.Perform();
                }

                _nextRespawnAttemptTime += 3f;
            }
        }

        public override void OnEnd()
        {
            RoR2Application.onFixedUpdate -= fixedUpdate;

            if (_spawnedMaster)
            {
                _spawnedMaster.TrueKill();
            }
        }
    }
}
