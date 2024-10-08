using EntityStates;
using RiskOfChaos.ModifierController.TimeScale;
using RiskOfChaos.Utilities.Extensions;
using RiskOfChaos.Utilities.Interpolation;
using RoR2;
using System;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Networking.Components
{
    public class SuperhotPlayerController : NetworkBehaviour
    {
        class TimeScaleModificationProvider : ITimeScaleModificationProvider, IDisposable
        {
            static int _activeProviderCount = 0;

            public event Action OnValueDirty;

            readonly SuperhotPlayerController _playerController;

            float _currentMultiplier = 1f;
            public float CurrentMultiplier
            {
                get => _currentMultiplier;
                private set
                {
                    if (_currentMultiplier == value)
                        return;

                    _currentMultiplier = value;
                    OnValueDirty?.Invoke();
                }
            }

            public float TargetMultiplier = 1f;

            bool _disposed;

            public TimeScaleModificationProvider(SuperhotPlayerController playerController)
            {
                _playerController = playerController;
                _activeProviderCount++;

                TimeScaleModificationManager.Instance.RegisterModificationProvider(this, ValueInterpolationFunctionType.EaseInOut, 0.5f);

                RoR2Application.onUpdate += update;
            }

            void update()
            {
                if (PauseStopController.instance && PauseStopController.instance.isPaused)
                    return;

                const float TIME_SCALE_CHANGE_UP_MAX_DELTA = 1f;
                const float TIME_SCALE_CHANGE_DOWN_MAX_DELTA = 2f;
                float maxDelta = CurrentMultiplier > TargetMultiplier ? TIME_SCALE_CHANGE_DOWN_MAX_DELTA : TIME_SCALE_CHANGE_UP_MAX_DELTA;

                CurrentMultiplier = Mathf.MoveTowards(CurrentMultiplier, TargetMultiplier, maxDelta * Time.unscaledDeltaTime);
            }

            public void ModifyValue(ref float value)
            {
                if (_activeProviderCount <= 0)
                    return;

                // Splits influence semi-fairly between all active instances, allowing over/under values proportional to active instance count
                value *= Mathf.Pow(_currentMultiplier, (1f + ((_activeProviderCount - 1) / 5f)) / _activeProviderCount);
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                _disposed = true;

                InterpolationState unregisterInterpolation = null;

                if (TimeScaleModificationManager.Instance)
                {
                    unregisterInterpolation = TimeScaleModificationManager.Instance.UnregisterModificationProvider(this, ValueInterpolationFunctionType.EaseInOut, 0.5f);
                }

                if (unregisterInterpolation != null)
                {
                    unregisterInterpolation.OnFinish += onUnregister;
                }
                else
                {
                    onUnregister();
                }

                void onUnregister()
                {
                    _activeProviderCount--;
                }

                RoR2Application.onUpdate -= update;
            }
        }

        NetworkedBodyAttachment _networkedBodyAttachment;

        Vector3 _lastBodyPosition;
        CharacterBody _body;

        TimeScaleModificationProvider _timeScaleModificationProviderServer;

        float _lastSetTargetMultiplier = 1f;

        void Awake()
        {
            _networkedBodyAttachment = GetComponent<NetworkedBodyAttachment>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            _timeScaleModificationProviderServer = new TimeScaleModificationProvider(this);
        }

        void OnDestroy()
        {
            _timeScaleModificationProviderServer?.Dispose();
            _timeScaleModificationProviderServer = null;
        }

        void Update()
        {
            if (PauseStopController.instance && PauseStopController.instance.isPaused)
                return;

            CharacterBody body = _networkedBodyAttachment.attachedBody;
            if (body)
            {
                if (!body.hasEffectiveAuthority)
                    return;

                if (_body != body)
                {
                    _lastBodyPosition = body.footPosition;
                }
            }
            else
            {
                if (!Util.HasEffectiveAuthority(gameObject))
                    return;
            }

            _body = body;

            float deltaTime = Time.unscaledDeltaTime;
            if (deltaTime <= 0f)
                return;

            float targetTimeScaleMultiplier = getTargetTimeScaleMultiplier(deltaTime);
            if (targetTimeScaleMultiplier != _lastSetTargetMultiplier)
            {
                _lastSetTargetMultiplier = targetTimeScaleMultiplier;
                CmdSetTargetTimeScaleMultiplier(targetTimeScaleMultiplier);
            }
        }

        bool shouldConsiderMovement()
        {
            if (!_body || !_body.healthComponent || !_body.healthComponent.alive)
                return false;

            EntityStateMachine bodyStateMachine = EntityStateMachine.FindByCustomName(_body.gameObject, "Body");
            if (bodyStateMachine && !bodyStateMachine.IsInMainState() && !bodyStateMachine.CurrentStateInheritsFrom(typeof(BaseCharacterMain)))
                return false;

            return true;
        }

        float getTargetTimeScaleMultiplier(float deltaTime)
        {
            if (!shouldConsiderMovement())
                return 1f;

            Vector3 currentPosition = _body.footPosition;
            float distanceMoved = Vector3.Distance(_lastBodyPosition, currentPosition);
            float velocity = distanceMoved / deltaTime;

            _lastBodyPosition = currentPosition;

            const float TIME_SCALE_MULTIPLIER = 0.95f;
            const float MIN_TIME_SCALE_MULTIPLIER = 0.15f;
            const float MAX_TIME_SCALE_MULTIPLIER = 1.7f;
            const float TIME_SCALE_COEFFICIENT = TIME_SCALE_MULTIPLIER * (MAX_TIME_SCALE_MULTIPLIER - MIN_TIME_SCALE_MULTIPLIER) / MAX_TIME_SCALE_MULTIPLIER;

            float maxSpeed = _body.moveSpeed;
            if (!_body.isSprinting)
                maxSpeed *= _body.sprintingSpeedMultiplier;

            float unscaledMultiplier = velocity / maxSpeed;
            float scaledMultiplier = (TIME_SCALE_COEFFICIENT * unscaledMultiplier) + MIN_TIME_SCALE_MULTIPLIER;

            return Mathf.Clamp(scaledMultiplier, MIN_TIME_SCALE_MULTIPLIER, MAX_TIME_SCALE_MULTIPLIER);
        }

        [Command]
        void CmdSetTargetTimeScaleMultiplier(float targetMultiplier)
        {
            _timeScaleModificationProviderServer.TargetMultiplier = targetMultiplier;
        }
    }
}
