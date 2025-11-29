using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Trackers;
using RiskOfChaos.Utilities;
using RiskOfOptions.OptionConfigs;
using RoR2;
using RoR2.ContentManagement;
using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Spawn
{
    [ChaosTimedEffect("spawn_brother_haunt", 45f, DefaultSelectionWeight = 0.7f, AllowDuplicates = false)]
    public sealed class SpawnBrotherHaunt : MonoBehaviour
    {
        static GameObject _brotherHauntPrefab;

        [SystemInitializer(typeof(MasterCatalog))]
        static void Init()
        {
            _brotherHauntPrefab = MasterCatalog.FindMasterPrefab("BrotherHauntMaster");
            if (!_brotherHauntPrefab)
            {
                Log.Error("Failed to find brother haunt master prefab");
            }
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

        AssetOrDirectReference<GameObject> _countdownTimerPrefabReference;

        public ChaosEffectComponent EffectComponent { get; private set; }

        float _masterRespawnTimer;
        CharacterMaster _spawnedMaster;

        float _hudUpdateTimer;

        public event Action DestroyTimersSignal;

        void Awake()
        {
            EffectComponent = GetComponent<ChaosEffectComponent>();

            if (NetworkClient.active)
            {
                _countdownTimerPrefabReference = new AssetOrDirectReference<GameObject>()
                {
                    unloadType = AsyncReferenceHandleUnloadType.AtWill,
                    address = new AssetReferenceGameObject(AddressableGuids.RoR2_Base_UI_HudCountdownPanel_prefab)
                };

                _countdownTimerPrefabReference.onValidReferenceDiscovered += _ =>
                {
                    updateHud();
                };

                _showCountdownTimer.SettingChanged += onShowCountdownTimerChanged;
                Stage.onStageStartGlobal += onStageStartGlobal;
            }
        }

        void OnDestroy()
        {
            if (_spawnedMaster)
            {
                _spawnedMaster.TrueKill();
            }

            _countdownTimerPrefabReference?.Reset();
            _countdownTimerPrefabReference = null;

            _showCountdownTimer.SettingChanged -= onShowCountdownTimerChanged;
            Stage.onStageStartGlobal -= onStageStartGlobal;
        }

        void onStageStartGlobal(Stage obj)
        {
            updateHud();
        }

        void onShowCountdownTimerChanged(object sender, ConfigChangedArgs<bool> args)
        {
            updateHud();
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
                        teamIndexOverride = TeamIndex.Monster
                    }.Perform();
                }
            }
        }

        void updateClient()
        {
            _hudUpdateTimer -= Time.deltaTime;
            if (_hudUpdateTimer <= 0f)
            {
                _hudUpdateTimer = 5f;
                updateHud();
            }
        }

        void updateHud()
        {
            bool canShowCountdownTimer = EffectComponent.DurationComponent && EffectComponent.DurationComponent.Duration > 0f;

            if (canShowCountdownTimer && _showCountdownTimer.Value && isValidScene())
            {
                if (_countdownTimerPrefabReference != null && _countdownTimerPrefabReference.IsLoaded())
                {
                    GameObject countdownTimerPrefab = _countdownTimerPrefabReference.Result;

                    List<HudCountdownPanelTracker> hudCountdownPanels = InstanceTracker.GetInstancesList<HudCountdownPanelTracker>();

                    foreach (HUD hud in HUD.readOnlyInstanceList)
                    {
                        if (hudCountdownPanels.Any(p => p.HUD == hud))
                            continue;

                        if (!hud.TryGetComponent(out ChildLocator childLocator))
                            continue;

                        RectTransform topCenterCluster = childLocator.FindChild("TopCenterCluster") as RectTransform;
                        if (!topCenterCluster)
                            continue;

                        GameObject countdownPanel = GameObject.Instantiate(countdownTimerPrefab, topCenterCluster);
                        CountdownTimerController countdownController = countdownPanel.AddComponent<CountdownTimerController>();
                        countdownController.OwnerEffect = this;

                        Log.Debug($"Created countdown timer for local user {hud.localUserViewer?.id}");
                    }
                }
            }
            else
            {
                DestroyTimersSignal?.Invoke();
            }
        }

        sealed class CountdownTimerController : MonoBehaviour
        {
            ChaosEffectComponent _ownerEffectComponent;
            public SpawnBrotherHaunt OwnerEffect
            {
                get => field;
                set
                {
                    if (field == value)
                        return;

                    if (_ownerEffectComponent)
                    {
                        _ownerEffectComponent.OnEffectEnd -= onOwnerEffectEnd;
                    }

                    if (field)
                    {
                        field.DestroyTimersSignal -= destroyTimer;
                    }

                    field = value;
                    _ownerEffectComponent = field ? field.EffectComponent : null;

                    if (_ownerEffectComponent)
                    {
                        _ownerEffectComponent.OnEffectEnd += onOwnerEffectEnd;
                    }

                    if (field)
                    {
                        field.DestroyTimersSignal += destroyTimer;
                    }
                }
            }

            RunTimeStamp _startTime;

            TimerText _timerText;

            void Awake()
            {
                _timerText = GetComponent<TimerText>();
                _startTime = RunTimeStamp.Now(RunTimerType.Realtime);
            }

            void OnDestroy()
            {
                OwnerEffect = null;
            }

            void FixedUpdate()
            {
                if (NetworkClient.active)
                {
                    if (_timerText)
                    {
                        float timeRemaining = _startTime.TimeSinceClamped;
                        if (_ownerEffectComponent)
                        {
                            timeRemaining = _ownerEffectComponent.TimeStarted.TimeSinceClamped;
                            if (_ownerEffectComponent.DurationComponent && _ownerEffectComponent.DurationComponent.Duration > 0f)
                            {
                                timeRemaining = _ownerEffectComponent.DurationComponent.Remaining;
                            }
                        }

                        _timerText.seconds = timeRemaining;
                    }
                }
            }

            void onOwnerEffectEnd(ChaosEffectComponent effectComponent)
            {
                Destroy(gameObject);
            }

            void destroyTimer()
            {
                Destroy(gameObject);
            }
        }
    }
}
