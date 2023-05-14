using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System;
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

        float _lastActivationTime;
        float _nextActivationTime;

        float _period;
        public float Period
        {
            readonly get => _period;
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

            return Mathf.CeilToInt((currentTime - _nextActivationTime) / Period);
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
            return currentTime >= _nextActivationTime;
        }

        public readonly float GetTimeRemaining()
        {
            return _nextActivationTime - currentTime;
        }
    }
}
