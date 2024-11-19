using BepInEx.Logging;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace RiskOfChaos
{
    internal static class Log
    {
        static readonly object _stringBuilderLock = new object();
        static readonly StringBuilder _sharedStringBuilder = new StringBuilder(256);

        static readonly int _cachedCallerPathPrefixLength;

        static ManualLogSource _logSource;

        static Log()
        {
            _cachedCallerPathPrefixLength = getCallerPathPrefixLength();

            static int getCallerPathPrefixLength([CallerFilePath] string callerPath = null)
            {
                const string MOD_NAME = nameof(RiskOfChaos) + @"\";

                int modNameLastPathIndex = callerPath.LastIndexOf(MOD_NAME);
                if (modNameLastPathIndex >= 0)
                {
                    return modNameLastPathIndex + MOD_NAME.Length;
                }
                else
                {
                    UnityEngine.Debug.LogError($"[{Main.PluginName}] Logger failed to determine caller path prefix length");
                    return 0;
                }
            }
        }

        internal static void Init(ManualLogSource logSource)
        {
            _logSource = logSource;
        }

        static StringBuilder AppendCallerPrefix(this StringBuilder stringBuilder, string callerPath, string callerMemberName, int callerLineNumber)
        {
            return stringBuilder.Append(callerPath, _cachedCallerPathPrefixLength, callerPath.Length - _cachedCallerPathPrefixLength)
                                .Append(':').Append(callerLineNumber)
                                .Append(" (").Append(callerMemberName).Append("):");
        }

        static StringBuilder buildCallerLogString(string callerPath, string callerMemberName, int callerLineNumber, object data)
        {
            lock (_stringBuilderLock)
            {
                return _sharedStringBuilder.Clear()
                                           .AppendCallerPrefix(callerPath, callerMemberName, callerLineNumber)
                                           .Append(' ')
                                           .Append(data);
            }
        }

        [Conditional("DEBUG")]
        internal static void Debug(object data, [CallerFilePath] string callerPath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = -1)
        {
            _logSource.LogDebug(buildCallerLogString(callerPath, callerMemberName, callerLineNumber, data));
        }

        [Conditional("DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Debug_NoCallerPrefix(object data)
        {
            _logSource.LogDebug(data);
        }

        internal static void Error(object data, [CallerFilePath] string callerPath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = -1)
        {
            _logSource.LogError(buildCallerLogString(callerPath, callerMemberName, callerLineNumber, data));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Error_NoCallerPrefix(object data)
        {
            _logSource.LogError(data);
        }

        internal static void Fatal(object data, [CallerFilePath] string callerPath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = -1)
        {
            _logSource.LogFatal(buildCallerLogString(callerPath, callerMemberName, callerLineNumber, data));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Fatal_NoCallerPrefix(object data)
        {
            _logSource.LogFatal(data);
        }

        internal static void Info(object data, [CallerFilePath] string callerPath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = -1)
        {
            _logSource.LogInfo(buildCallerLogString(callerPath, callerMemberName, callerLineNumber, data));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Info_NoCallerPrefix(object data)
        {
            _logSource.LogInfo(data);
        }

        internal static void Message(object data, [CallerFilePath] string callerPath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = -1)
        {
            _logSource.LogMessage(buildCallerLogString(callerPath, callerMemberName, callerLineNumber, data));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Message_NoCallerPrefix(object data)
        {
            _logSource.LogMessage(data);
        }

        internal static void Warning(object data, [CallerFilePath] string callerPath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = -1)
        {
            _logSource.LogWarning(buildCallerLogString(callerPath, callerMemberName, callerLineNumber, data));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Warning_NoCallerPrefix(object data)
        {
            _logSource.LogWarning(data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool ShouldLog(LogLevel logLevel)
        {
#if !DEBUG
            if ((logLevel & LogLevel.Debug) != 0)
                return false;
#endif

            return logLevel > LogLevel.None;
        }

        internal static void LogType(LogLevel logLevel, object data)
        {
            if (!ShouldLog(logLevel))
                return;

            _logSource.Log(logLevel, data);
        }
    }
}