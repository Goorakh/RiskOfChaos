using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RoR2;
using RoR2.UI;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosTimedEffect("spawn_brother_haunt", 45f, DefaultSelectionWeight = 0.7f, IsNetworked = true)]
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

        readonly List<TimerText> _countdownTimers = new List<TimerText>();

        public override void OnStart()
        {
            RoR2Application.onFixedUpdate += fixedUpdate;

            if (TimedType == TimedEffectType.FixedDuration)
            {
                foreach (HUD hud in HUD.readOnlyInstanceList)
                {
                    if (!hud.TryGetComponent(out ChildLocator childLocator))
                        continue;

                    RectTransform topCenterCluster = childLocator.FindChild("TopCenterCluster") as RectTransform;
                    if (!topCenterCluster)
                        continue;

                    GameObject countdownPanel = GameObject.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/UI/HudModules/HudCountdownPanel"), topCenterCluster);
                    _countdownTimers.Add(countdownPanel.GetComponent<TimerText>());
                }
            }
        }

        float _nextRespawnAttemptTime = 0f;
        void fixedUpdate()
        {
            if (NetworkServer.active)
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

            float timeRemaining = TimeRemaining;
            foreach (TimerText countdownTimer in _countdownTimers)
            {
                countdownTimer.seconds = timeRemaining;
            }
        }

        public override void OnEnd()
        {
            RoR2Application.onFixedUpdate -= fixedUpdate;

            foreach (TimerText countdownTimer in _countdownTimers)
            {
                GameObject.Destroy(countdownTimer.gameObject);
            }

            _countdownTimers.Clear();

            if (NetworkServer.active)
            {
                if (_spawnedMaster)
                {
                    _spawnedMaster.TrueKill();
                }
            }
        }
    }
}
