using RoR2;
using System;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling.EffectComponents
{
    [RequireComponent(typeof(ChaosEffectComponent))]
    public sealed class ChaosEffectDurationComponent : NetworkBehaviour
    {
        ChaosEffectComponent _effectComponent;

        [SyncVar]
        public TimedEffectType TimedType;

        [SyncVar]
        public int NumStagesCompletedWhileActive;

        [SyncVar]
        public float Duration;

        public float Elapsed
        {
            get
            {
                switch (TimedType)
                {
                    case TimedEffectType.UntilStageEnd:
                        return NumStagesCompletedWhileActive;
                    case TimedEffectType.FixedDuration:
                        return _effectComponent.TimeStarted.timeSinceClamped;
                    case TimedEffectType.Permanent:
                    case TimedEffectType.AlwaysActive:
                        return 0f;
                    default:
                        throw new NotImplementedException($"Timed type {TimedType} is not implemented");
                }
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

            TimedEffectInfo effectInfo = _effectComponent.ChaosEffectInfo as TimedEffectInfo;
            if (effectInfo == null)
            {
                Log.Error($"EffectDurationComponent used on non-timed effect {_effectComponent.ChaosEffectInfo} ({name})");
                return;
            }

            TimedType = effectInfo.TimedType;
            Duration = effectInfo.Duration;
        }

        void FixedUpdate()
        {
            if (NetworkServer.active)
            {
                fixedUpdateServer();
            }
        }

        [Server]
        void onServerStageComplete(Stage stage)
        {
            NumStagesCompletedWhileActive++;
        }

        [Server]
        void fixedUpdateServer()
        {
            if (Elapsed >= Duration)
            {
#if DEBUG
                Log.Debug($"Ending timed effect {_effectComponent.ChaosEffectInfo} (id={netId})");
#endif

                NetworkServer.Destroy(gameObject);
            }
        }
    }
}
