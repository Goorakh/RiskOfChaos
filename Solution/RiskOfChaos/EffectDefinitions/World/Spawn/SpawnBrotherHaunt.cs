using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Trackers;
using RiskOfOptions.OptionConfigs;
using RoR2;
using RoR2.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosTimedEffect("spawn_brother_haunt", 45f, DefaultSelectionWeight = 0.7f, IsNetworked = true, AllowDuplicates = false)]
    public sealed class SpawnBrotherHaunt : TimedEffect
    {
        [EffectConfig]
        static readonly ConfigHolder<bool> _showCountdownTimer =
            ConfigFactory<bool>.CreateConfig("Display Countdown Timer", true)
                               .Description("Displays the moon escape sequence timer during the effect")
                               .OptionConfig(new CheckBoxConfig())
                               .Build();

        static readonly GameObject _brotherHauntPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/BrotherHaunt/BrotherHauntMaster.prefab").WaitForCompletion();

        static readonly GameObject _countdownTimerPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/UI/HudCountdownPanel.prefab").WaitForCompletion();

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

        bool _canShowCountdownTimer;
        readonly List<TimerText> _countdownTimers = [];

        public override void OnStart()
        {
            RoR2Application.onFixedUpdate += fixedUpdate;

            _canShowCountdownTimer = TimedType == TimedEffectType.FixedDuration && _countdownTimerPrefab;
        }

        static bool isValidScene()
        {
            SceneDef currentScene = SceneCatalog.GetSceneDefForCurrentScene();
            if (!currentScene)
                return false;

            switch (currentScene.sceneType)
            {
                case SceneType.Menu:
                case SceneType.Cutscene:
                    return false;
            }

            return true;
        }

        void fixedUpdate()
        {
            if (NetworkServer.active)
            {
                if ((!_spawnedMaster || _spawnedMaster.IsDeadAndOutOfLivesServer()) && isValidScene())
                {
#if DEBUG
                    Log.Debug("Spawned master is null or dead, respawning...");
#endif

                    _spawnedMaster = _brotherHauntSummon.Perform();
                }
            }

            if (_canShowCountdownTimer && _showCountdownTimer.Value && isValidScene())
            {
                float timeRemaining = TimeRemaining;

                for (int i = _countdownTimers.Count - 1; i >= 0; i--)
                {
                    if (_countdownTimers[i])
                    {
                        _countdownTimers[i].seconds = timeRemaining;
                    }
                    else
                    {
                        _countdownTimers.RemoveAt(i);
                    }
                }

                if (_countdownTimers.Count < HUD.readOnlyInstanceList.Count)
                {
                    foreach (HUD hud in HUD.readOnlyInstanceList)
                    {
                        if (InstanceTracker.GetInstancesList<HudCountdownPanelTracker>().Any(p => p.HUD == hud))
                            continue;

                        if (!hud.TryGetComponent(out ChildLocator childLocator))
                            continue;

                        RectTransform topCenterCluster = childLocator.FindChild("TopCenterCluster") as RectTransform;
                        if (!topCenterCluster)
                            continue;

                        GameObject countdownPanel = GameObject.Instantiate(_countdownTimerPrefab, topCenterCluster);
                        TimerText timerText = countdownPanel.GetComponent<TimerText>();
                        timerText.seconds = timeRemaining;

                        _countdownTimers.Add(timerText);

#if DEBUG
                        Log.Debug($"Created countdown timer for local user {hud.localUserViewer?.id}");
#endif
                    }
                }
            }
            else
            {
                if (_countdownTimers.Count > 0)
                {
                    destroyAllCountdownTimers();
                }
            }
        }

        void destroyAllCountdownTimers()
        {
            foreach (TimerText countdownTimer in _countdownTimers)
            {
                if (countdownTimer)
                {
                    GameObject.Destroy(countdownTimer.gameObject);
                }
            }

            _countdownTimers.Clear();

#if DEBUG
            Log.Debug("Removed local countdown timer(s)");
#endif
        }

        public override void OnEnd()
        {
            RoR2Application.onFixedUpdate -= fixedUpdate;

            destroyAllCountdownTimers();

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
