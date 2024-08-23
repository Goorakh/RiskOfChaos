using RoR2;
using System;

namespace RiskOfChaos.EffectHandling
{
    public class CompletePeriodicRunTimer : IRunTimer
    {
        public event Action OnActivate;

        readonly TimerFlags _flags;

        RunTimerType _lastTimerType;
        float _lastTimerTimeRemaining;

        PeriodicRunTimer _stopwatchEffectDispatchTimer;
        PeriodicRunTimer _realtimeEffectDispatchTimer;

        ref PeriodicRunTimer currentTimer
        {
            get
            {
                Run run = Run.instance;
                if (!run)
                {
                    Log.Warning("No run instance, using unpaused timer");
                    return ref _stopwatchEffectDispatchTimer;
                }

                if (run.isRunStopwatchPaused)
                {
                    return ref _realtimeEffectDispatchTimer;
                }
                else
                {
                    return ref _stopwatchEffectDispatchTimer;
                }
            }
        }

        public float Period
        {
            get
            {
                return currentTimer.Period;
            }
            set
            {
                _stopwatchEffectDispatchTimer.Period = value;
                _realtimeEffectDispatchTimer.Period = value;
            }
        }

        public CompletePeriodicRunTimer(float period, TimerFlags flags = TimerFlags.None)
        {
            _stopwatchEffectDispatchTimer = new PeriodicRunTimer(RunTimerType.Stopwatch, period);
            _realtimeEffectDispatchTimer = new PeriodicRunTimer(RunTimerType.Realtime, period);

            _flags = flags;

            _lastTimerType = currentTimer.TimeType;
        }

        public void SkipAllScheduledActivations()
        {
            ref PeriodicRunTimer timer = ref currentTimer;
            timer.SkipAllScheduledActivations();
        }

        public void RewindScheduledActivations(float numSeconds)
        {
            _stopwatchEffectDispatchTimer.RewindScheduledActivations(numSeconds);
            _realtimeEffectDispatchTimer.RewindScheduledActivations(numSeconds);
        }

        public int GetNumScheduledActivations()
        {
            ref PeriodicRunTimer timer = ref currentTimer;
            return timer.GetNumScheduledActivations();
        }

        public void SkipActivations(int numActivationsToSkip)
        {
            currentTimer.SkipActivations(numActivationsToSkip);
        }

        public void Update()
        {
            ref PeriodicRunTimer timer = ref currentTimer;

            updateTimer(ref timer);

            if (timer.ShouldActivate())
            {
                timer.SkipAllScheduledActivations();

#if DEBUG
                Log.Debug($"Timer activating (time remaining: {timer.GetTimeRemaining()})");
#endif

                if (timer.GetTimeRemaining() <= 1f / 20f)
                {
#if DEBUG
                    Log.Debug($"Prevented double timer activation");
#endif

                    timer.SkipActivations(1);
                }

                OnActivate?.Invoke();
            }
        }

        void updateTimer(ref PeriodicRunTimer timer)
        {
            if (_lastTimerType != timer.TimeType)
            {
                if (timer.TimeType == RunTimerType.Realtime)
                {
                    // Skip all the activations that should have already happened, but didn't since this timer hasn't updated
                    timer.SkipAllScheduledActivations();
                }
                else
                {
                    int scheduledActivations = timer.GetNumScheduledActivations();
                    if (scheduledActivations > 1)
                        timer.SkipActivations(scheduledActivations - 1);
                }

                if ((_flags & TimerFlags.EnforcePeriodOnTimerSwitch) != 0)
                {
                    if (timer.GetTimeRemaining() < _lastTimerTimeRemaining && Period >= _lastTimerTimeRemaining)
                    {
                        timer.SkipAllScheduledActivations();

#if DEBUG
                        Log.Debug("Skipped timer activation(s) to not trigger timer early");
#endif
                    }
                }
            }

            _lastTimerType = timer.TimeType;
            _lastTimerTimeRemaining = timer.GetTimeRemaining();
        }

        public float GetTimeRemaining()
        {
            return currentTimer.GetTimeRemaining();
        }

        public float GetLastActivationTimeStopwatch()
        {
            return _stopwatchEffectDispatchTimer.GetLastActivationTime();
        }

        public void SetLastActivationTimeStopwatch(float lastActivationTime)
        {
            _stopwatchEffectDispatchTimer.SetLastActivationTime(lastActivationTime);
            _realtimeEffectDispatchTimer.SkipAllScheduledActivations();
        }
    }
}
