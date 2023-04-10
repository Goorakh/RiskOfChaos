using RoR2;
using System;

namespace RiskOfChaos.EffectHandling
{
    public struct PeriodicRunTimer
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

                return TimeType switch
                {
                    RunTimerType.Unpaused => run.GetRunStopwatch(),
                    RunTimerType.Paused => run.fixedTime,
                    _ => throw new NotImplementedException($"Timer type {TimeType} is not implemented")
                };
            }
        }

        float _lastActivationTime;
        float _nextActivationTime;

        float _period;
        public float Period
        {
            get => _period;
            set
            {
                if (_period == value)
                    return;

                _period = value;

                if (_lastActivationTime >= 0f)
                {
#if DEBUG
                    float oldNextActivationTime = _nextActivationTime;
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
            _lastActivationTime = -1f;
            _nextActivationTime = 0f;
        }

        public void ScheduleNextActivation()
        {
            _lastActivationTime = _nextActivationTime;
            _nextActivationTime += _period;

#if DEBUG
            Log.Debug($"{nameof(_lastActivationTime)}={_lastActivationTime}, {nameof(_nextActivationTime)}={_nextActivationTime}");
#endif
        }

        public readonly bool ShouldActivate()
        {
            return currentTime >= _nextActivationTime;
        }

        public readonly float GetTimeRemaining()
        {
            return _nextActivationTime - currentTime;
        }
    }
}
