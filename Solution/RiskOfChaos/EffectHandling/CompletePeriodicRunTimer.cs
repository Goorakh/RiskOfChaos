using RiskOfChaos.Utilities;
using RoR2;

namespace RiskOfChaos.EffectHandling
{
    public class CompletePeriodicRunTimer : IRunTimer
    {
        public delegate void TimerActivateDelegate(RunTimeStamp activationTime);
        public event TimerActivateDelegate OnActivate;

        bool _wasRunStopwatchPausedLastUpdate = false;

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

        public CompletePeriodicRunTimer(float period)
        {
            _stopwatchEffectDispatchTimer = new PeriodicRunTimer(RunTimerType.Stopwatch, period);
            _realtimeEffectDispatchTimer = new PeriodicRunTimer(RunTimerType.Realtime, period);
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

            updateStopwatchPaused(ref timer);

            if (timer.ShouldActivate())
            {
                timer.SkipAllScheduledActivations();

                Log.Debug($"Timer activating (time remaining: {timer.GetNextActivationTime().TimeUntil})");

                if (timer.GetNextActivationTime().TimeUntil <= 1f / 20f)
                {
                    Log.Debug($"Prevented double timer activation");

                    timer.SkipActivations(1);
                }

                OnActivate?.Invoke(timer.GetLastActivationTime());
            }
        }

        void updateStopwatchPaused(ref PeriodicRunTimer timer)
        {
            bool isStopwatchPaused = Run.instance.isRunStopwatchPaused;
            if (_wasRunStopwatchPausedLastUpdate != isStopwatchPaused)
            {
                _wasRunStopwatchPausedLastUpdate = isStopwatchPaused;

                if (isStopwatchPaused)
                {
                    // Skip all the activations that should have already happened, but didn't since this timer hasn't updated
                    while (timer.ShouldActivate())
                    {
                        timer.ScheduleNextActivation();
                    }
                }
            }
        }

        public RunTimeStamp GetNextActivationTime()
        {
            return currentTimer.GetNextActivationTime();
        }

        public RunTimeStamp GetLastActivationTimeStopwatch()
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
