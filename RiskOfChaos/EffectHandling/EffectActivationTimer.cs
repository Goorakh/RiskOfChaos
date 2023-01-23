using RiskOfChaos.Config;
using RoR2;
using System;

namespace RiskOfChaos.EffectHandling
{
    public struct EffectDispatchTimer
    {
        public readonly EffectDispatchTimerType TimeType;

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
                    EffectDispatchTimerType.Unpaused => run.GetRunStopwatch(),
                    EffectDispatchTimerType.Paused => run.fixedTime,
                    _ => throw new NotImplementedException($"Timer type {TimeType} is not implemented")
                };
            }
        }

        float _lastDispatchTime;
        float _nextDispatchTime;

        public EffectDispatchTimer(EffectDispatchTimerType timerType)
        {
            TimeType = timerType;
            Reset();
        }

        public void Reset()
        {
            _lastDispatchTime = -1f;
            _nextDispatchTime = 0f;
        }

        public void OnTimeBetweenEffectsChanged()
        {
            if (_lastDispatchTime < 0f)
                return;

#if DEBUG
            float oldNextEffectTime = _nextDispatchTime;
#endif

            _nextDispatchTime = _lastDispatchTime + Configs.General.TimeBetweenEffects;

#if DEBUG
            Log.Debug($"({TimeType}) {nameof(_nextDispatchTime)}: {oldNextEffectTime} -> {_nextDispatchTime}");
#endif
        }

        public void ScheduleNextDispatch()
        {
            _lastDispatchTime = _nextDispatchTime;
            _nextDispatchTime += Configs.General.TimeBetweenEffects;

#if DEBUG
            Log.Debug($"{nameof(_lastDispatchTime)}={_lastDispatchTime}, {nameof(_nextDispatchTime)}={_nextDispatchTime}");
#endif
        }

        public readonly bool ShouldActivate()
        {
            return currentTime >= _nextDispatchTime;
        }
    }
}
