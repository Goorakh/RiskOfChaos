using System;

namespace RiskOfTwitch.Logging
{
    internal class ConsoleLogSource : ILogSource
    {
        public void Log(object message, LogType type)
        {
#if !DEBUG
            if (type == LogType.Debug)
                return;
#endif

            Console.WriteLine(message);
        }
    }
}
