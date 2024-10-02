using RoR2;

namespace RiskOfChaos.Utilities.Extensions
{
    public static class TimerQueueExtensions
    {
        public static void Clear(this TimerQueue timerQueue)
        {
            for (int i = timerQueue.count - 1; i >= 0; i--)
            {
                timerQueue.RemoveTimerAt(i);
            }
        }
    }
}
