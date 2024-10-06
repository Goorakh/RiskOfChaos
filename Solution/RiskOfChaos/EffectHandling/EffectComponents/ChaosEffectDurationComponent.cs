using RiskOfChaos.EffectHandling.EffectClassAttributes;
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

        public TimedEffectType TimedType
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (TimedEffectType)_timedTypeInternal;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _timedTypeInternal = (int)value;
        }

        [SyncVar]
        public int NumStagesCompletedWhileActive;

        [SyncVar]
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
        }

        void OnEnable()
        {
            Stage.onServerStageComplete += onServerStageComplete;
        }

        void OnDisable()
        {
            Stage.onServerStageComplete -= onServerStageComplete;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            if (Duration <= 0f)
            {
                Log.Error($"No duration defined for effect {name} ({netId})");
                EndEffect();
            }
        }

        void FixedUpdate()
        {
            if (NetworkServer.active)
            {
                fixedUpdateServer();
            }
        }

        [Server]
        void fixedUpdateServer()
        {
            if (Elapsed >= Duration)
            {
#if DEBUG
                Log.Debug($"Ending timed effect {name} (id={netId})");
#endif

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
            _effectComponent.RetireEffect();
        }
    }
}
