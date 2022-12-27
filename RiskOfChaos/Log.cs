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

        static string getLogPrefix(string callerPath, int callerLineNumber)
        {
            const string MOD_NAME = "RiskOfChaos";

            int modNameLastPathIndex = callerPath.LastIndexOf(MOD_NAME);
            if (modNameLastPathIndex >= 0)
            {
                callerPath = callerPath.Substring(modNameLastPathIndex + MOD_NAME.Length + 1);
            }

            return $"{callerPath}:{callerLineNumber} ";
        }

#if DEBUG
        internal static void Debug(object data, bool includeCallerInfo = true, [CallerFilePath] string callerPath = default, [CallerLineNumber] int callerLineNumber = -1) => _logSource.LogDebug(includeCallerInfo ? getLogPrefix(callerPath, callerLineNumber) + data : data);
#endif
        internal static void Error(object data, bool includeCallerInfo = true, [CallerFilePath] string callerPath = default, [CallerLineNumber] int callerLineNumber = -1) => _logSource.LogError(includeCallerInfo ? getLogPrefix(callerPath, callerLineNumber) + data : data);
        internal static void Fatal(object data, bool includeCallerInfo = true, [CallerFilePath] string callerPath = default, [CallerLineNumber] int callerLineNumber = -1) => _logSource.LogFatal(includeCallerInfo ? getLogPrefix(callerPath, callerLineNumber) + data : data);
        internal static void Info(object data, bool includeCallerInfo = true, [CallerFilePath] string callerPath = default, [CallerLineNumber] int callerLineNumber = -1) => _logSource.LogInfo(includeCallerInfo ? getLogPrefix(callerPath, callerLineNumber) + data : data);
        internal static void Message(object data, bool includeCallerInfo = true, [CallerFilePath] string callerPath = default, [CallerLineNumber] int callerLineNumber = -1) => _logSource.LogMessage(includeCallerInfo ? getLogPrefix(callerPath, callerLineNumber) + data : data);
        internal static void Warning(object data, bool includeCallerInfo = true, [CallerFilePath] string callerPath = default, [CallerLineNumber] int callerLineNumber = -1) => _logSource.LogWarning(includeCallerInfo ? getLogPrefix(callerPath, callerLineNumber) + data : data);
    }
}