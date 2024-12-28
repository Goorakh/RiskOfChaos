using RiskOfChaos.Content;
using RiskOfChaos.SaveHandling;
using RoR2;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling.EffectComponents
{
    [DisallowMultipleComponent]
    [RequiredComponents(typeof(ChaosEffectComponent))]
    public sealed class ChaosEffectDurationComponent : NetworkBehaviour, IEffectHUDVisibilityProvider
    {
        ChaosEffectComponent _effectComponent;

        public TimedEffectInfo TimedEffectInfo => _effectComponent ? _effectComponent.ChaosEffectInfo as TimedEffectInfo : null;

        ObjectSerializationComponent _serializationComponent;

        bool _effectEnded;

        bool _isInSceneTransition;

        [SyncVar]
        int _timedTypeInternal;

        [SerializedMember("t")]
        public TimedEffectType TimedType
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (TimedEffectType)_timedTypeInternal;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _timedTypeInternal = (int)value;
        }

        [SyncVar]
        [SerializedMember("sc")]
        public int NumStagesCompletedWhileActive;

        [SyncVar]
        [SerializedMember("d")]
        public float Duration = -1f;

        public float Elapsed
        {
            get
            {
                switch (TimedType)
                {
                    case TimedEffectType.UntilStageEnd:
                        return NumStagesCompletedWhileActive;
                    case TimedEffectType.FixedDuration:
                        return _effectComponent.TimeStarted.TimeSinceClamped;
                    case TimedEffectType.Permanent:
                    case TimedEffectType.AlwaysActive:
                        return 0f;
                    default:
                        throw new NotImplementedException($"Timed type {TimedType} is not implemented");
                }
            }
        }

        public float Remaining => Mathf.Max(0f, Duration - Elapsed);

        bool IEffectHUDVisibilityProvider.CanShowOnHUD
        {
            get
            {
                if (TimedEffectInfo == null || !TimedEffectInfo.GetShouldDisplayOnHUD(TimedType))
                    return false;

                return true;
            }
        }

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
            _effectComponent.EffectDestructionHandledByComponent = true;

            _serializationComponent = GetComponent<ObjectSerializationComponent>();
        }

        void OnEnable()
        {
            Stage.onServerStageComplete += onServerStageComplete;
            Stage.onStageStartGlobal += onStageStartGlobal;
        }

        void OnDisable()
        {
            Stage.onServerStageComplete -= onServerStageComplete;
            Stage.onStageStartGlobal -= onStageStartGlobal;
        }

        void FixedUpdate()
        {
            if (!_effectComponent)
                return;

            if (NetworkServer.active)
            {
                fixedUpdateServer();
            }
        }

        [Server]
        void fixedUpdateServer()
        {
            checkElapsed();
        }

        [Server]
        void checkElapsed()
        {
            if (Elapsed >= Duration)
            {
                if (Duration <= 0f)
                {
                    Log.Error($"No duration defined for effect {name} ({netId})");
                }

                if (!_effectEnded)
                {
                    if (_serializationComponent)
                    {
                        _serializationComponent.enabled = false;
                    }
                }

                _effectEnded = true;
            }

            if (_effectEnded && !SceneExitController.isRunning && !_isInSceneTransition)
            {
                EndEffect();
            }
        }

        [Server]
        void onServerStageComplete(Stage stage)
        {
            NumStagesCompletedWhileActive++;
            _isInSceneTransition = true;
        }

        void onStageStartGlobal(Stage stage)
        {
            if (NetworkServer.active)
            {
                _isInSceneTransition = false;
            }
        }

        [Server]
        public void EndEffect()
        {
            Log.Debug($"Ending timed effect {name} (id={netId})");
            _effectComponent.RetireEffect();
        }
    }
}
