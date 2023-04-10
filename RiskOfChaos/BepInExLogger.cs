using Microsoft.Extensions.Logging;
using System;

namespace RiskOfChaos
{
    internal class BepInExLogger<T> : ILogger<T>
    {
        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return RiskOfChaos.Log.ShouldLog(toBepInExLogLevel(logLevel));
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            RiskOfChaos.Log.LogType(toBepInExLogLevel(logLevel), $"{eventId}: {formatter(state, exception)}");
        }

        static BepInEx.Logging.LogLevel toBepInExLogLevel(LogLevel logLevel)
        {
            return logLevel switch
            {
                LogLevel.Trace or LogLevel.Debug => BepInEx.Logging.LogLevel.Debug,
                LogLevel.Information => BepInEx.Logging.LogLevel.Info,
                LogLevel.Warning => BepInEx.Logging.LogLevel.Warning,
                LogLevel.Error => BepInEx.Logging.LogLevel.Error,
                LogLevel.Critical => BepInEx.Logging.LogLevel.Fatal,
                LogLevel.None => BepInEx.Logging.LogLevel.None,
                _ => throw new NotImplementedException(),
            };
        }
    }
}