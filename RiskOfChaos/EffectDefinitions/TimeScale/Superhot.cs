using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.ModifierController.TimeScale;
using RiskOfChaos.Utilities;
using RoR2;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.TimeScale
{
    [ChaosEffect("superhot")]
    [ChaosTimedEffect(TimedEffectType.UntilStageEnd, AllowDuplicates = false)]
    public sealed class Superhot : TimedEffect
    {
        class PlayerTimeMovementTracker : MonoBehaviour, ITimeScaleModificationProvider
        {
            public event Action OnValueDirty;

            public bool ContributeToPlayerRealtimeTimeScalePatch => false;

            CharacterMaster _master;

            float _lastTimeScaleMultiplier;
            float _currentTimeScaleMultiplier;

            Vector3 _lastPosition;

            void Awake()
            {
                _master = GetComponent<CharacterMaster>();
                _master.onBodyStart += onBodyStart;
            }

            void OnEnable()
            {
                InstanceTracker.Add(this);

                if (NetworkServer.active && TimeScaleModificationManager.Instance)
                {
                    TimeScaleModificationManager.Instance.RegisterModificationProvider(this);
                }

                _lastTimeScaleMultiplier = 1f;
                _currentTimeScaleMultiplier = 1f;
            }

            void OnDisable()
            {
                InstanceTracker.Remove(this);

                if (NetworkServer.active && TimeScaleModificationManager.Instance)
                {
                    TimeScaleModificationManager.Instance.UnregisterModificationProvider(this);
                }
            }

            void onBodyStart(CharacterBody body)
            {
                _lastPosition = body.footPosition;
            }

            void FixedUpdate()
            {
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
                value *= Mathf.Pow(_currentTimeScaleMultiplier, 1f / InstanceTracker.GetInstancesList<PlayerTimeMovementTracker>().Count);
            }
        }

        public override void OnStart()
        {
            foreach (CharacterMaster playerMaster in PlayerUtils.GetAllPlayerMasters(true))
            {
                setComponentOn(playerMaster, true);
            }

            PlayerCharacterMasterController.onPlayerAdded += PlayerCharacterMasterController_onPlayerAdded;
            PlayerCharacterMasterController.onPlayerRemoved += PlayerCharacterMasterController_onPlayerRemoved;
        }

        static void setComponentOn(CharacterMaster playerMaster, bool active)
        {
            if (playerMaster.TryGetComponent(out PlayerTimeMovementTracker movementTracker))
            {
                movementTracker.enabled = active;
            }
            else
            {
                if (active)
                {
                    playerMaster.gameObject.AddComponent<PlayerTimeMovementTracker>();
                }
            }
        }

        static void PlayerCharacterMasterController_onPlayerAdded(PlayerCharacterMasterController playerController)
        {
            setComponentOn(playerController.master, true);
        }

        static void PlayerCharacterMasterController_onPlayerRemoved(PlayerCharacterMasterController playerController)
        {
            setComponentOn(playerController.master, false);
        }

        public override void OnEnd()
        {
            InstanceUtils.DestroyAllTrackedInstances<PlayerTimeMovementTracker>();

            PlayerCharacterMasterController.onPlayerAdded -= PlayerCharacterMasterController_onPlayerAdded;
            PlayerCharacterMasterController.onPlayerRemoved -= PlayerCharacterMasterController_onPlayerRemoved;
        }
    }
}
