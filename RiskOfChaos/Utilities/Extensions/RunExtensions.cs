using RiskOfChaos.EffectHandling;
using RoR2;
using System;

namespace RiskOfChaos.Utilities.Extensions
{
    public static class RunExtensions
    {
        public static float GetRunTime(this Run run, RunTimerType timerType)
        {
            return timerType switch
            {
                RunTimerType.Stopwatch => run.GetRunStopwatch(),
                RunTimerType.Realtime => run.fixedTime,
                _ => throw new NotImplementedException($"Timer type {timerType} is not implemented"),
            };
        }
    }
}
