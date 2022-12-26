using BepInEx.Logging;
using System.Runtime.CompilerServices;

namespace RiskOfChaos
{
    internal static class Log
    {
        internal static ManualLogSource _logSource;

        internal static void Init(ManualLogSource logSource)
        {
            _logSource = logSource;
        }

#if DEBUG
        internal static void Debug(object data, bool includeCallerInfo = true, [CallerFilePath] string callerPath = default, [CallerLineNumber] int callerLineNumber = -1) => _logSource.LogDebug(includeCallerInfo ? $"{callerPath}:{callerLineNumber} {data}" : data);
#endif
        internal static void Error(object data, bool includeCallerInfo = true, [CallerFilePath] string callerPath = default, [CallerLineNumber] int callerLineNumber = -1) => _logSource.LogError(includeCallerInfo ? $"{callerPath}:{callerLineNumber} {data}" : data);
        internal static void Fatal(object data, bool includeCallerInfo = true, [CallerFilePath] string callerPath = default, [CallerLineNumber] int callerLineNumber = -1) => _logSource.LogFatal(includeCallerInfo ? $"{callerPath}:{callerLineNumber} {data}" : data);
        internal static void Info(object data, bool includeCallerInfo = true, [CallerFilePath] string callerPath = default, [CallerLineNumber] int callerLineNumber = -1) => _logSource.LogInfo(includeCallerInfo ? $"{callerPath}:{callerLineNumber} {data}" : data);
        internal static void Message(object data, bool includeCallerInfo = true, [CallerFilePath] string callerPath = default, [CallerLineNumber] int callerLineNumber = -1) => _logSource.LogMessage(includeCallerInfo ? $"{callerPath}:{callerLineNumber} {data}" : data);
        internal static void Warning(object data, bool includeCallerInfo = true, [CallerFilePath] string callerPath = default, [CallerLineNumber] int callerLineNumber = -1) => _logSource.LogWarning(includeCallerInfo ? $"{callerPath}:{callerLineNumber} {data}" : data);
    }
}