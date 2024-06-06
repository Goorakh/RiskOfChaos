using BepInEx.Logging;
using RiskOfTwitch.Logging;
using System;

namespace RiskOfChaos
{
    internal sealed class TwitchLibLogSource : RiskOfTwitch.Logging.ILogSource
    {
        public void Log(object message, LogType type)
        {
            LogLevel logLevel = type switch
            {
                LogType.Debug => LogLevel.Debug,
                LogType.Info => LogLevel.Info,
                LogType.Message => LogLevel.Message,
                LogType.Warning => LogLevel.Warning,
                LogType.Error => LogLevel.Error,
                LogType.Fatal => LogLevel.Fatal,
                _ => throw new NotImplementedException($"Log type {type} is not implemented"),
            };

            RiskOfChaos.Log.LogType(logLevel, message);
        }
    }
}
