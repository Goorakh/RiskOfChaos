using RoR2;

namespace RiskOfChaos.Utilities.Extensions
{
    public static class TimerQueueExtensions
    {
        public static void Clear(this TimerQueue timerQueue)
        {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
            for (int i = timerQueue.count - 1; i >= 0; i--)
            {
                timerQueue.RemoveTimerAt(i);
            }
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
        }
    }
}
