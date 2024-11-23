using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Trackers;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RoR2;
using RoR2.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosTimedEffect("spawn_brother_haunt", 45f, DefaultSelectionWeight = 0.7f, AllowDuplicates = false)]
    public sealed class SpawnBrotherHaunt : MonoBehaviour
    {
        static GameObject _brotherHauntPrefab;

        static GameObject _countdownTimerPrefab;

        [SystemInitializer]
        static void Init()
        {
            AsyncOperationHandle<GameObject> brotherHauntMasterLoad = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/BrotherHaunt/BrotherHauntMaster.prefab");
            brotherHauntMasterLoad.OnSuccess(brotherHauntMasterPrefab => _brotherHauntPrefab = brotherHauntMasterPrefab);

            AsyncOperationHandle<GameObject> hudCountdownPanelLoad = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/UI/HudCountdownPanel.prefab");
            hudCountdownPanelLoad.OnSuccess(hudCountdownPanelPrefab => _countdownTimerPrefab = hudCountdownPanelPrefab);
        }

        [EffectConfig]
        static readonly ConfigHolder<bool> _showCountdownTimer =
            ConfigFactory<bool>.CreateConfig("Display Countdown Timer", true)
                               .Description("Displays the moon escape sequence timer during the effect")
                               .OptionConfig(new CheckBoxConfig())
                               .Build();

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _brotherHauntPrefab;
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

        ChaosEffectDurationComponent _durationComponent;

        float _masterRespawnTimer;
        CharacterMaster _spawnedMaster;

        readonly List<TimerText> _countdownTimers = [];
        readonly List<OnDestroyCallback> _destroyCallbacks = [];

        bool _trackedObjectDestroyed;

        void Awake()
        {
            _durationComponent = GetComponent<ChaosEffectDurationComponent>();
        }

        void OnDestroy()
        {
            foreach (OnDestroyCallback destroyCallback in _destroyCallbacks)
            {
                if (destroyCallback)
                {
                    OnDestroyCallback.RemoveCallback(destroyCallback);
                }
            }

            _destroyCallbacks.Clear();

            foreach (TimerText countdownTimer in _countdownTimers)
            {
                if (countdownTimer)
                {
                    Destroy(countdownTimer.gameObject);
                }
            }

            _countdownTimers.Clear();

            if (_spawnedMaster)
            {
                _spawnedMaster.TrueKill();
            }
        }

        void FixedUpdate()
        {
            if (_trackedObjectDestroyed)
            {
                _trackedObjectDestroyed = false;

                UnityObjectUtils.RemoveAllDestroyed(_destroyCallbacks);

                int countdownTimersRemoved = UnityObjectUtils.RemoveAllDestroyed(_countdownTimers);
                Log.Debug($"Cleared {countdownTimersRemoved} destroyed countdown timer(s)");
            }

            if (NetworkServer.active)
            {
                updateServer();
            }

            if (NetworkClient.active)
            {
                updateClient();
            }
        }

        void updateServer()
        {
            if (isValidScene())
            {
                if (!_spawnedMaster || _spawnedMaster.IsDeadAndOutOfLivesServer())
                {
                    _masterRespawnTimer -= Time.fixedDeltaTime;
                    if (_masterRespawnTimer <= 0f && Stage.instance && Stage.instance.entryTime.timeSinceClamped > 1f)
                    {
                        Log.Debug("Spawned master is null or dead, respawning...");

                        _spawnedMaster = new MasterSummon
                        {
                            masterPrefab = _brotherHauntPrefab,
                            ignoreTeamMemberLimit = true,
                            teamIndexOverride = TeamIndex.Lunar
                        }.Perform();
                    }
                }
                else
                {
                    _masterRespawnTimer = 2.5f;
                }
            }
        }

        void updateClient()
        {
            bool canShowCountdownTimer = false;
            float timeRemaining = 0f;

            if (_durationComponent && _durationComponent.TimedType == EffectHandling.TimedEffectType.FixedDuration)
            {
                canShowCountdownTimer = true;
                timeRemaining = _durationComponent.Remaining;
            }

            if (canShowCountdownTimer && _showCountdownTimer.Value && isValidScene())
            {
                if (_countdownTimers.Count < HUD.readOnlyInstanceList.Count)
                {
                    _countdownTimers.EnsureCapacity(HUD.readOnlyInstanceList.Count);
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

                        _countdownTimers.Add(timerText);

                        OnDestroyCallback destroyCallback = OnDestroyCallback.AddCallback(countdownPanel, _ =>
                        {
                            _trackedObjectDestroyed = true;
                        });

                        _destroyCallbacks.Add(destroyCallback);

                        Log.Debug($"Created countdown timer for local user {hud.localUserViewer?.id}");
                    }
                }

                foreach (TimerText timerText in _countdownTimers)
                {
                    if (timerText)
                    {
                        timerText.seconds = timeRemaining;
                    }
                }
            }
            else
            {
                foreach (TimerText countdownTimer in _countdownTimers)
                {
                    if (countdownTimer)
                    {
                        Destroy(countdownTimer.gameObject);
                    }
                }
            }
        }
    }
}
