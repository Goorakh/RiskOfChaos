using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.EffectHandling
{
    public struct PeriodicRunTimer : IRunTimer
    {
        public readonly RunTimerType TimeType;

        readonly float currentTime
        {
            get
            {
                Run run = Run.instance;
                if (!run)
                {
                    Log.Warning("no run instance");
                    return 0f;
                }

                return run.GetRunTime(TimeType);
            }
        }

        RunTimeStamp _lastActivationTime;
        RunTimeStamp _nextActivationTime;

        float _period;
        public float Period
        {
            readonly get => _period;
            set
            {
                if (_period == value)
                    return;

                _period = value;

                if (_lastActivationTime.Time >= 0f)
                {
#if DEBUG
                    RunTimeStamp oldNextActivationTime = _nextActivationTime;
#endif

                    _nextActivationTime = _lastActivationTime + _period;

#if DEBUG
                    Log.Debug($"({TimeType}) {nameof(_nextActivationTime)}: {oldNextActivationTime} -> {_nextActivationTime}");
#endif
                }
            }
        }

        public PeriodicRunTimer(RunTimerType timerType, float period)
        {
            TimeType = timerType;
            Period = period;

            Reset();
        }

        public void Reset()
        {
            _lastActivationTime = new RunTimeStamp(TimeType, -1f);
            _nextActivationTime = new RunTimeStamp(TimeType, 0f);
        }

        public void SetLastActivationTime(float value)
        {
            _lastActivationTime.Time = value;
            _nextActivationTime.Time = value >= 0f ? value + Period : 0f;
        }

        public void ScheduleNextActivation()
        {
            _lastActivationTime = _nextActivationTime;
            _nextActivationTime += Period;

#if DEBUG
            Log.Debug($"{nameof(_lastActivationTime)}={_lastActivationTime}, {nameof(_nextActivationTime)}={_nextActivationTime}");
#endif
        }

        public void SkipActivations(int numActivationsToSkip)
        {
            if (numActivationsToSkip < 0 && -(numActivationsToSkip * Period) > _nextActivationTime)
            {
                Reset();
            }
            else
            {
                _nextActivationTime += numActivationsToSkip * Period;
                _lastActivationTime = _nextActivationTime - Period;
            }

#if DEBUG
            Log.Debug($"{nameof(_lastActivationTime)}={_lastActivationTime}, {nameof(_nextActivationTime)}={_nextActivationTime}");
#endif
        }

        public readonly int GetNumScheduledActivations()
        {
            if (!ShouldActivate())
                return 0;

            return Mathf.CeilToInt(_nextActivationTime.TimeSince / Period);
        }

        public void SkipAllScheduledActivations()
        {
            SkipActivations(GetNumScheduledActivations());
        }

        public void RewindScheduledActivations(float numSeconds)
        {
            if (numSeconds >= currentTime)
            {
                Reset();
                SkipActivations(1);
            }
            else
            {
                int activationsToRewind = Mathf.CeilToInt(numSeconds / Period);
                SkipActivations(-activationsToRewind);
            }
        }

        public readonly bool ShouldActivate()
        {
            return _nextActivationTime.HasPassed;
        }

        public readonly RunTimeStamp GetNextActivationTime()
        {
            return _nextActivationTime;
        }

        public readonly RunTimeStamp GetLastActivationTime()
        {
            return _lastActivationTime;
        }
    }
}
