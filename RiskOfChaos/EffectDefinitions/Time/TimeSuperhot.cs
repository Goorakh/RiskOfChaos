﻿using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.ModifierController.TimeScale;
using RiskOfChaos.Utilities;
using RoR2;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Time
{
    [ChaosEffect("superhot")]
    [ChaosTimedEffect(TimedEffectType.UntilStageEnd, AllowDuplicates = false)]
    public sealed class TimeSuperhot : TimedEffect
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
                float deltaTime = UnityEngine.Time.fixedUnscaledDeltaTime;
                float targetTimeScaleMultiplier = getCurrentTimeScaleMultiplier(deltaTime);

                const float TIME_SCALE_CHANGE_UP_MAX_DELTA = 2f;
                const float TIME_SCALE_CHANGE_DOWN_MAX_DELTA = 3f;

                float maxDelta = _currentTimeScaleMultiplier > targetTimeScaleMultiplier ? TIME_SCALE_CHANGE_DOWN_MAX_DELTA : TIME_SCALE_CHANGE_UP_MAX_DELTA;

                _currentTimeScaleMultiplier = Mathf.MoveTowards(_currentTimeScaleMultiplier, targetTimeScaleMultiplier, maxDelta * deltaTime);

#if DEBUG
                Log.Debug($"{Util.GetBestMasterName(_master)}: {nameof(_lastTimeScaleMultiplier)}={_lastTimeScaleMultiplier}, {nameof(_currentTimeScaleMultiplier)}={_currentTimeScaleMultiplier}, {nameof(targetTimeScaleMultiplier)}={targetTimeScaleMultiplier}");
#endif

                if (_currentTimeScaleMultiplier != _lastTimeScaleMultiplier)
                {
                    _lastTimeScaleMultiplier = _currentTimeScaleMultiplier;
                    OnValueDirty?.Invoke();
                }
            }

            float getCurrentTimeScaleMultiplier(float deltaTime)
            {
                if (_master)
                {
                    CharacterBody body = _master.GetBody();
                    if (body && body.healthComponent && body.healthComponent.alive)
                    {
                        float distanceMoved = Vector3.Distance(_lastPosition, body.footPosition);
                        float velocity = distanceMoved / deltaTime;

                        _lastPosition = body.footPosition;

                        const float TIME_SCALE_MULTIPLIER = 0.95f;
                        const float MIN_TIME_SCALE_MULTIPLIER = 0.1f;
                        const float MAX_TIME_SCALE_MULTIPLIER = 1.75f;
                        const float TIME_SCALE_COEFFICIENT = TIME_SCALE_MULTIPLIER * (MAX_TIME_SCALE_MULTIPLIER - MIN_TIME_SCALE_MULTIPLIER) / MAX_TIME_SCALE_MULTIPLIER;

                        float moveSpeed = body.moveSpeed;
                        if (!body.isSprinting)
                            moveSpeed *= body.sprintingSpeedMultiplier;

                        float unscaledMultiplier = velocity / moveSpeed;
                        float scaledMultiplier = (TIME_SCALE_COEFFICIENT * unscaledMultiplier) + MIN_TIME_SCALE_MULTIPLIER;

#if DEBUG
                        Log.Debug($"{Util.GetBestMasterName(_master)}: {nameof(distanceMoved)}={distanceMoved}, {nameof(deltaTime)}={deltaTime}, {nameof(velocity)}={velocity}, {nameof(unscaledMultiplier)}={unscaledMultiplier}, {nameof(scaledMultiplier)}={scaledMultiplier}");
#endif

                        return Mathf.Clamp(scaledMultiplier, MIN_TIME_SCALE_MULTIPLIER, MAX_TIME_SCALE_MULTIPLIER);
                    }
                }

                return 1f;
            }

            public void ModifyValue(ref float value)
            {
                value *= _currentTimeScaleMultiplier;
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
            foreach (PlayerTimeMovementTracker movementTracker in InstanceTracker.GetInstancesList<PlayerTimeMovementTracker>().ToList())
            {
                GameObject.Destroy(movementTracker);
            }

            PlayerCharacterMasterController.onPlayerAdded -= PlayerCharacterMasterController_onPlayerAdded;
            PlayerCharacterMasterController.onPlayerRemoved -= PlayerCharacterMasterController_onPlayerRemoved;
        }
    }
}