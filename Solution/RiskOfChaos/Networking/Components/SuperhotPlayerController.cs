using EntityStates;
using RiskOfChaos.Content;
using RiskOfChaos.ModificationController;
using RiskOfChaos.ModificationController.TimeScale;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.Networking.Components
{
    [RequiredComponents(typeof(ValueModificationController))]
    public class SuperhotPlayerController : NetworkBehaviour, ITimeScaleModificationProvider
    {
        static readonly List<SuperhotPlayerController> _instances = [];

        NetworkIdentity _networkIdentity;

        ValueModificationController _modificationController;

        NetworkedBodyAttachment _networkedBodyAttachment;

        bool _hasEffectiveAuthority;

        Vector3 _lastBodyPosition;
        CharacterBody _body;

        [SyncVar(hook = nameof(setCurrentMultiplier))]
        float _currentMultiplier = 1f;
        float _targetMultiplier = 1f;

        float _lastSetTargetMultiplier = 1f;

        void Awake()
        {
            _networkIdentity = GetComponent<NetworkIdentity>();

            _modificationController = GetComponent<ValueModificationController>();

            _networkedBodyAttachment = GetComponent<NetworkedBodyAttachment>();

            _instances.Add(this);
        }

        void OnDestroy()
        {
            _instances.Remove(this);
        }

        void Start()
        {
            updateHasAuthority();
        }

        public override void OnStartAuthority()
        {
            base.OnStartAuthority();

            updateHasAuthority();
        }

        public override void OnStopAuthority()
        {
            base.OnStopAuthority();

            updateHasAuthority();
        }

        void updateHasAuthority()
        {
            _hasEffectiveAuthority = Util.HasEffectiveAuthority(_networkIdentity);
        }

        void Update()
        {
            if (PauseStopController.instance && PauseStopController.instance.isPaused)
                return;

            bool hasAuthority = false;

            CharacterBody body = _networkedBodyAttachment.attachedBody;
            if (body)
            {
                hasAuthority = body.hasEffectiveAuthority;
            }
            else
            {
                hasAuthority = _hasEffectiveAuthority;
            }

            float deltaTime = Time.unscaledDeltaTime;

            if (hasAuthority)
            {
                updateAuthority(body, deltaTime);
            }

            if (NetworkServer.active)
            {
                updateServer(deltaTime);
            }
        }

        void updateAuthority(CharacterBody currentBody, float deltaTime)
        {
            if (_body != currentBody)
            {
                _lastBodyPosition = currentBody.footPosition;
            }

            _body = currentBody;

            if (deltaTime > 0f)
            {
                float targetTimeScaleMultiplier = getTargetTimeScaleMultiplier(deltaTime);
                if (targetTimeScaleMultiplier != _lastSetTargetMultiplier)
                {
                    _lastSetTargetMultiplier = targetTimeScaleMultiplier;
                    CmdSetTargetTimeScaleMultiplier(targetTimeScaleMultiplier);
                }
            }
        }

        [Server]
        void updateServer(float deltaTime)
        {
            if (deltaTime <= 0f)
                return;

            if (_currentMultiplier != _targetMultiplier)
            {
                if (_modificationController && !_modificationController.IsRetired)
                {
                    const float TIME_SCALE_CHANGE_UP_MAX_DELTA = 1f;
                    const float TIME_SCALE_CHANGE_DOWN_MAX_DELTA = 2f;
                    float maxDelta = _currentMultiplier > _targetMultiplier ? TIME_SCALE_CHANGE_DOWN_MAX_DELTA : TIME_SCALE_CHANGE_UP_MAX_DELTA;

                    _currentMultiplier = Mathf.MoveTowards(_currentMultiplier, _targetMultiplier, maxDelta * deltaTime);
                }
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
            _targetMultiplier = targetMultiplier;
        }

        float getCurrentMultiplier()
        {
            float currentMultiplier = _currentMultiplier;

            if (_modificationController && _modificationController.IsInterpolating)
            {
                currentMultiplier = Mathf.Lerp(1f, currentMultiplier, Ease.InOutQuad(_modificationController.CurrentInterpolationFraction));
            }

            return currentMultiplier;
        }

        public bool TryGetTimeScaleModification(out TimeScaleModificationInfo modificationInfo)
        {
            int activeInstancesCount = _instances.Count;
            if (activeInstancesCount <= 0)
            {
                Log.Error("Attempting to modify time scale with 0 active instances");
                modificationInfo = default;
                return false;
            }

            modificationInfo = new TimeScaleModificationInfo
            {
                TimeScaleMultiplier = Mathf.Pow(getCurrentMultiplier(), (1f + ((activeInstancesCount - 1) / 5f)) / activeInstancesCount),
                CompensatePlayerSpeed = false
            };

            return true;
        }

        [Server]
        public void Retire()
        {
            if (_modificationController)
            {
                _modificationController.Retire();
            }
            else
            {
                NetworkServer.Destroy(gameObject);
            }
        }

        void markModificationsDirty()
        {
            if (_modificationController)
            {
                _modificationController.InvokeOnValuesDirty();
            }
        }

        void setCurrentMultiplier(float currentMultiplier)
        {
            _currentMultiplier = currentMultiplier;
            markModificationsDirty();
        }
    }
}
