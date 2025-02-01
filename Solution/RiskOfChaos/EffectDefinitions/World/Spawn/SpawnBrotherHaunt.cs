using RiskOfChaos.Collections;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Trackers;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using RoR2;
using RoR2.UI;
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

        [SystemInitializer(typeof(MasterCatalog))]
        static void Init()
        {
            _brotherHauntPrefab = MasterCatalog.FindMasterPrefab("BrotherHauntMaster");
            if (!_brotherHauntPrefab)
            {
                Log.Error("Failed to find brother haunt master prefab");
            }

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

        ChaosEffectComponent _effectComponent;
        ChaosEffectDurationComponent _durationComponent;

        float _masterRespawnTimer;
        CharacterMaster _spawnedMaster;

        readonly ClearingObjectList<TimerText> _countdownTimers = new ClearingObjectList<TimerText>()
        {
            ObjectIdentifier = "SpawnBrotherHauntCountdownTimers",
            DestroyComponentGameObject = true
        };

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
            _durationComponent = GetComponent<ChaosEffectDurationComponent>();
        }

        void OnDestroy()
        {
            _countdownTimers.ClearAndDispose(true);

            if (_spawnedMaster)
            {
                _spawnedMaster.TrueKill();
            }
        }

        void FixedUpdate()
        {
            if (NetworkServer.active)
            {
                updateServer();
            }

            if (NetworkClient.active)
            {
                updateClient();
            }
        }

        void Update()
        {
            updateTimers();
        }

        void updateServer()
        {
            _masterRespawnTimer -= Time.fixedDeltaTime;
            if (_masterRespawnTimer <= 0f)
            {
                _masterRespawnTimer = 2.5f;
                if ((!_spawnedMaster || _spawnedMaster.IsDeadAndOutOfLivesServer()) && isValidScene() && Stage.instance && Stage.instance.entryTime.timeSinceClamped > 1f)
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
        }

        void updateClient()
        {
            bool canShowCountdownTimer = _durationComponent && _durationComponent.TimedType == TimedEffectType.FixedDuration;

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

                        Log.Debug($"Created countdown timer for local user {hud.localUserViewer?.id}");
                    }
                }
            }
            else
            {
                _countdownTimers.Clear(true);
            }
        }

        void updateTimers()
        {
            if (_countdownTimers.Count > 0)
            {
                float timeRemaining = _effectComponent.TimeStarted.TimeSinceClamped;
                if (_durationComponent && _durationComponent.TimedType == TimedEffectType.FixedDuration)
                {
                    timeRemaining = _durationComponent.Remaining;
                }

                foreach (TimerText timerText in _countdownTimers)
                {
                    if (timerText)
                    {
                        timerText.seconds = timeRemaining;
                    }
                }
            }
        }
    }
}
