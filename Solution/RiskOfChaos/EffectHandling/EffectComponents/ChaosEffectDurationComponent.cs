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

        public float Remaining
        {
            get
            {
                return Mathf.Max(0f, Duration - Elapsed);
            }
            set
            {
                Duration = Elapsed + value;
            }
        }

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
        }

        void OnEnable()
        {
            Stage.onServerStageComplete += onServerStageComplete;
        }

        void OnDisable()
        {
            Stage.onServerStageComplete -= onServerStageComplete;
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
                    Log.Error($"No duration defined for effect {Util.GetGameObjectHierarchyName(gameObject)} ({netId})");
                }

                EndEffect();
            }
        }

        [Server]
        void onServerStageComplete(Stage stage)
        {
            NumStagesCompletedWhileActive++;
        }

        [Server]
        public void EndEffect()
        {
            Log.Debug($"Ending timed effect {Util.GetGameObjectHierarchyName(gameObject)} (id={netId})");
            _effectComponent.RetireEffect();
        }
    }
}
