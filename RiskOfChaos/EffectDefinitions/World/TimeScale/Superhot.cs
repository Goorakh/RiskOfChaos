using EntityStates;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.ModifierController.TimeScale;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RiskOfChaos.Utilities.Interpolation;
using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.TimeScale
{
    [ChaosTimedEffect("superhot", 45f, AllowDuplicates = false)]
    public sealed class Superhot : TimedEffect
    {
        class PlayerTimeMovementTracker : MonoBehaviour, ITimeScaleModificationProvider
        {
            public event Action OnValueDirty;

            CharacterMaster _master;

            float _lastTimeScaleMultiplier;
            float _currentTimeScaleMultiplier;

            Vector3 _lastPosition;

            void Awake()
            {
                _master = GetComponent<CharacterMaster>();
                _master.onBodyStart += onBodyStart;

                if (NetworkServer.active && TimeScaleModificationManager.Instance)
                {
                    TimeScaleModificationManager.Instance.RegisterModificationProvider(this);
                }
            }

            void OnEnable()
            {
                InstanceTracker.Add(this);

                _lastTimeScaleMultiplier = 1f;
                _currentTimeScaleMultiplier = 1f;
            }

            void OnDisable()
            {
                InstanceTracker.Remove(this);
            }

            void onBodyStart(CharacterBody body)
            {
                _lastPosition = body.footPosition;
            }

            void FixedUpdate()
            {
                if (NetworkServer.dontListen && PauseManager.isPaused)
                    return;

                float deltaTime = Time.fixedUnscaledDeltaTime;
                float targetTimeScaleMultiplier = getCurrentTimeScaleMultiplier(deltaTime);

                const float TIME_SCALE_CHANGE_UP_MAX_DELTA = 1f;
                const float TIME_SCALE_CHANGE_DOWN_MAX_DELTA = 2f;

                float maxDelta = _currentTimeScaleMultiplier > targetTimeScaleMultiplier ? TIME_SCALE_CHANGE_DOWN_MAX_DELTA : TIME_SCALE_CHANGE_UP_MAX_DELTA;

                _currentTimeScaleMultiplier = Mathf.MoveTowards(_currentTimeScaleMultiplier, targetTimeScaleMultiplier, maxDelta * deltaTime);

                if (_currentTimeScaleMultiplier != _lastTimeScaleMultiplier)
                {
                    _lastTimeScaleMultiplier = _currentTimeScaleMultiplier;
                    OnValueDirty?.Invoke();
                }
            }

            bool shouldConsiderMovement()
            {
                if (!_master)
                    return false;

                CharacterBody body = _master.GetBody();
                if (!body || !body.healthComponent || !body.healthComponent.alive)
                    return false;

                EntityStateMachine bodyStateMachine = EntityStateMachine.FindByCustomName(body.gameObject, "Body");
                if (bodyStateMachine && !bodyStateMachine.IsInMainState() && !bodyStateMachine.CurrentStateInheritsFrom(typeof(BaseCharacterMain)))
                    return false;

                return true;
            }

            float getCurrentTimeScaleMultiplier(float deltaTime)
            {
                if (!shouldConsiderMovement())
                    return 1f;

                CharacterBody body = _master.GetBody();
                Vector3 currentPosition = body.footPosition;
                float distanceMoved = Vector3.Distance(_lastPosition, currentPosition);
                float velocity = distanceMoved / deltaTime;

                _lastPosition = currentPosition;

                const float TIME_SCALE_MULTIPLIER = 0.95f;
                const float MIN_TIME_SCALE_MULTIPLIER = 0.15f;
                const float MAX_TIME_SCALE_MULTIPLIER = 1.7f;
                const float TIME_SCALE_COEFFICIENT = TIME_SCALE_MULTIPLIER * (MAX_TIME_SCALE_MULTIPLIER - MIN_TIME_SCALE_MULTIPLIER) / MAX_TIME_SCALE_MULTIPLIER;

                float moveSpeed = body.moveSpeed;
                if (!body.isSprinting)
                    moveSpeed *= body.sprintingSpeedMultiplier;

                float unscaledMultiplier = velocity / moveSpeed;
                float scaledMultiplier = (TIME_SCALE_COEFFICIENT * unscaledMultiplier) + MIN_TIME_SCALE_MULTIPLIER;

                return Mathf.Clamp(scaledMultiplier, MIN_TIME_SCALE_MULTIPLIER, MAX_TIME_SCALE_MULTIPLIER);
            }

            public void ModifyValue(ref float value)
            {
                // Splits influence semi-fairly between players, while keeping the exact min and max values regardless of player count
                value *= Mathf.Pow(_currentTimeScaleMultiplier, 1f / Mathf.Max(1, InstanceTracker.GetInstancesList<PlayerTimeMovementTracker>().Count));
            }

            public void Unregister()
            {
                if (!NetworkServer.active)
                {
                    Log.Warning("Called on client");
                    return;
                }

                InterpolationState interpolationState;
                if (TimeScaleModificationManager.Instance)
                {
                    interpolationState = TimeScaleModificationManager.Instance.UnregisterModificationProvider(this, ValueInterpolationFunctionType.EaseInOut, 0.5f);
                }
                else
                {
                    interpolationState = null;
                }

                if (interpolationState is not null)
                {
                    interpolationState.OnFinish += () =>
                    {
                        Destroy(this);
                    };
                }
                else
                {
                    Destroy(this);
                }
            }
        }

        readonly HashSet<PlayerTimeMovementTracker> _playerMovementTrackers = [];

        public override void OnStart()
        {
            foreach (CharacterMaster playerMaster in PlayerUtils.GetAllPlayerMasters(true))
            {
                setComponentOn(playerMaster, true);
            }

            PlayerCharacterMasterController.onPlayerAdded += PlayerCharacterMasterController_onPlayerAdded;
            PlayerCharacterMasterController.onPlayerRemoved += PlayerCharacterMasterController_onPlayerRemoved;
        }

        void setComponentOn(CharacterMaster playerMaster, bool active)
        {
            if (playerMaster.TryGetComponent(out PlayerTimeMovementTracker movementTracker))
            {
                movementTracker.enabled = active;
            }
            else
            {
                if (active)
                {
                    _playerMovementTrackers.Add(playerMaster.gameObject.AddComponent<PlayerTimeMovementTracker>());
                }
            }
        }

        void PlayerCharacterMasterController_onPlayerAdded(PlayerCharacterMasterController playerController)
        {
            setComponentOn(playerController.master, true);
        }

        void PlayerCharacterMasterController_onPlayerRemoved(PlayerCharacterMasterController playerController)
        {
            setComponentOn(playerController.master, false);
        }

        public override void OnEnd()
        {
            foreach (PlayerTimeMovementTracker movementTracker in _playerMovementTrackers)
            {
                movementTracker.Unregister();
            }
            _playerMovementTrackers.Clear();

            PlayerCharacterMasterController.onPlayerAdded -= PlayerCharacterMasterController_onPlayerAdded;
            PlayerCharacterMasterController.onPlayerRemoved -= PlayerCharacterMasterController_onPlayerRemoved;
        }
    }
}
