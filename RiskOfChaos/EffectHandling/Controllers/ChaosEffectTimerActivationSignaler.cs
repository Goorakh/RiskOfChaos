using RiskOfChaos.Config;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling.Controllers
{
    [ChaosController(true)]
    public class ChaosEffectTimerActivationSignaler : MonoBehaviour, IChaosEffectActivationSignaler
    {
        public event IChaosEffectActivationSignaler.SignalShouldDispatchEffectDelegate SignalShouldDispatchEffect;

        bool _wasRunStopwatchPausedLastUpdate = false;

        EffectDispatchTimer _unpausedEffectDispatchTimer = new EffectDispatchTimer(EffectDispatchTimerType.Unpaused);
        EffectDispatchTimer _pausedEffectDispatchTimer = new EffectDispatchTimer(EffectDispatchTimerType.Paused);

        public void SkipAllScheduledEffects()
        {
            ref EffectDispatchTimer dispatchTimer = ref currentEffectDispatchTimer;
            while (dispatchTimer.ShouldActivate())
            {
                dispatchTimer.ScheduleNextDispatch();
            }
        }

        ref EffectDispatchTimer currentEffectDispatchTimer
        {
            get
            {
                Run run = Run.instance;
                if (!run)
                {
                    Log.Warning("No run instance, using unpaused timer");
                    return ref _unpausedEffectDispatchTimer;
                }

                if (run.isRunStopwatchPaused)
                {
                    return ref _pausedEffectDispatchTimer;
                }
                else
                {
                    return ref _unpausedEffectDispatchTimer;
                }
            }
        }

        Xoroshiro128Plus _nextEffectRNG;

        void OnEnable()
        {
            RoR2Application.onUpdate += update;
            Configs.General.OnTimeBetweenEffectsChanged += onTimeBetweenEffectsConfigChanged;

            resetState();

            if (Run.instance)
            {
                _nextEffectRNG = new Xoroshiro128Plus(Run.instance.runRNG.nextUlong);
            }
        }

        void OnDisable()
        {
            RoR2Application.onUpdate -= update;
            Configs.General.OnTimeBetweenEffectsChanged -= onTimeBetweenEffectsConfigChanged;

            resetState();

            _nextEffectRNG = null;
        }

        void resetState()
        {
            _unpausedEffectDispatchTimer.Reset();
            _pausedEffectDispatchTimer.Reset();

            _wasRunStopwatchPausedLastUpdate = false;
        }

        void onTimeBetweenEffectsConfigChanged()
        {
            _pausedEffectDispatchTimer.OnTimeBetweenEffectsChanged();
            _unpausedEffectDispatchTimer.OnTimeBetweenEffectsChanged();
        }

        bool canDispatchEffects
        {
            get
            {
#if DEBUG
                if (Configs.General.DebugDisable)
                    return false;
#endif

                if (!NetworkServer.active)
                    return false;

                if (PauseManager.isPaused && NetworkServer.dontListen)
                    return false;

                if (SceneExitController.isRunning)
                    return false;

                if (!Run.instance || Run.instance.isGameOverServer)
                    return false;

                const float STAGE_START_OFFSET = 2f;
                if (!Stage.instance || Stage.instance.entryTime.timeSince < STAGE_START_OFFSET)
                    return false;

                return true;
            }
        }

        void update()
        {
            if (!canDispatchEffects)
                return;

            ref EffectDispatchTimer dispatchTimer = ref currentEffectDispatchTimer;

            updateStopwatchPaused(ref dispatchTimer);

            if (dispatchTimer.ShouldActivate())
            {
                dispatchTimer.ScheduleNextDispatch();
                dispatchRandomEffect();
            }
        }

        void updateStopwatchPaused(ref EffectDispatchTimer dispatchTimer)
        {
            bool isStopwatchPaused = Run.instance.isRunStopwatchPaused;
            if (_wasRunStopwatchPausedLastUpdate != isStopwatchPaused)
            {
                _wasRunStopwatchPausedLastUpdate = isStopwatchPaused;

                if (isStopwatchPaused)
                {
                    // Skip all the effect dispatches that should have already happened, but didn't since this timer hasn't updated
                    while (dispatchTimer.ShouldActivate())
                    {
                        dispatchTimer.ScheduleNextDispatch();
                    }
                }
            }
        }

        void dispatchRandomEffect(EffectDispatchFlags dispatchFlags = EffectDispatchFlags.None)
        {
            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return;
            }

            SignalShouldDispatchEffect?.Invoke(ChaosEffectCatalog.PickActivatableEffect(_nextEffectRNG), dispatchFlags);
        }
    }
}
