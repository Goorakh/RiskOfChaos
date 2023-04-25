using BepInEx.Configuration;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosEffect("spawn_brother_haunt", DefaultSelectionWeight = 0.7f)]
    public sealed class SpawnBrotherHaunt : TimedEffect
    {
        [InitEffectInfo]
        static readonly ChaosEffectInfo _effectInfo;

        static ConfigEntry<float> _effectDurationConfig;
        const float EFFECT_DURATION_DEFAULT_VALUE = 45f;

        static float effectDuration
        {
            get
            {
                if (_effectDurationConfig == null)
                {
                    return EFFECT_DURATION_DEFAULT_VALUE;
                }
                else
                {
                    return Mathf.Max(_effectDurationConfig.Value, 0f);
                }
            }
        }

        [SystemInitializer(typeof(ChaosEffectCatalog))]
        static void InitConfigs()
        {
            _effectDurationConfig = bindEffectDurationConfig(_effectInfo, EFFECT_DURATION_DEFAULT_VALUE, new StepSliderConfig
            {
                formatString = "{0:F0}",
                min = 0f,
                max = 120f,
                increment = 1f
            });
        }

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

        public override TimedEffectType TimedType => TimedEffectType.FixedDuration;

        protected override TimeSpan duration => TimeSpan.FromSeconds(effectDuration);

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
