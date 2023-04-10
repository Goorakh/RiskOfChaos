using RoR2;
using System;
using System.Collections.Generic;
using System.Text;

namespace RiskOfChaos.EffectHandling
{
    public class CompletePeriodicRunTimer
    {
        public event Action OnActivate;

        bool _wasRunStopwatchPausedLastUpdate = false;

        PeriodicRunTimer _unpausedEffectDispatchTimer;
        PeriodicRunTimer _pausedEffectDispatchTimer;

        ref PeriodicRunTimer currentTimer
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

        public float Period
        {
            get
            {
                return currentTimer.Period;
            }
            set
            {
                _unpausedEffectDispatchTimer.Period = value;
                _pausedEffectDispatchTimer.Period = value;
            }
        }

        public CompletePeriodicRunTimer(float period)
        {
            _unpausedEffectDispatchTimer = new PeriodicRunTimer(RunTimerType.Unpaused, period);
            _pausedEffectDispatchTimer = new PeriodicRunTimer(RunTimerType.Paused, period);
        }

        public void SkipAllScheduledActivations()
        {
            ref PeriodicRunTimer timer = ref currentTimer;
            while (timer.ShouldActivate())
            {
                timer.ScheduleNextActivation();
            }
        }

        public void Update()
        {
            ref PeriodicRunTimer timer = ref currentTimer;

            updateStopwatchPaused(ref timer);

            if (timer.ShouldActivate())
            {
                do
                {
                    timer.ScheduleNextActivation();
                } while (timer.ShouldActivate());

                OnActivate?.Invoke();
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

        public float GetTimeRemaining()
        {
            return currentTimer.GetTimeRemaining();
        }
    }
}
