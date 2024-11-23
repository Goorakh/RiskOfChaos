using RiskOfTwitch.Logging;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace RiskOfTwitch
{
    public static class Log
    {
        public static ILogSource LogSource { get; set; } = new ConsoleLogSource();

        static readonly object _stringBuilderLock = new object();
        static readonly StringBuilder _sharedStringBuilder = new StringBuilder(256);

        static readonly int _cachedCallerPathPrefixLength;

        static Log()
        {
            _cachedCallerPathPrefixLength = getCallerPathPrefixLength();

            static int getCallerPathPrefixLength([CallerFilePath] string callerPath = null)
            {
                const string PATH_NAME = "RiskOfTwitch";

                int pathNameLastPathIndex = callerPath.LastIndexOf(PATH_NAME);
                if (pathNameLastPathIndex >= 0)
                {
                    return pathNameLastPathIndex;
                }
                else
                {
                    Error_NoCallerPrefix($"[{PATH_NAME}] Logger failed to determine caller path prefix length");
                    return 0;
                }
            }
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
            LogSource.Log(buildCallerLogString(callerPath, callerMemberName, callerLineNumber, data), LogType.Debug);
        }

        [Conditional("DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Debug_NoCallerPrefix(object data)
        {
            LogSource.Log(data, LogType.Debug);
        }

        internal static void Error(object data, [CallerFilePath] string callerPath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = -1)
        {
            LogSource.Log(buildCallerLogString(callerPath, callerMemberName, callerLineNumber, data), LogType.Error);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Error_NoCallerPrefix(object data)
        {
            LogSource.Log(data, LogType.Error);
        }

        internal static void Fatal(object data, [CallerFilePath] string callerPath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = -1)
        {
            LogSource.Log(buildCallerLogString(callerPath, callerMemberName, callerLineNumber, data), LogType.Fatal);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Fatal_NoCallerPrefix(object data)
        {
            LogSource.Log(data, LogType.Fatal);
        }

        internal static void Info(object data, [CallerFilePath] string callerPath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = -1)
        {
            LogSource.Log(buildCallerLogString(callerPath, callerMemberName, callerLineNumber, data), LogType.Info);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Info_NoCallerPrefix(object data)
        {
            LogSource.Log(data, LogType.Info);
        }

        internal static void Message(object data, [CallerFilePath] string callerPath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = -1)
        {
            LogSource.Log(buildCallerLogString(callerPath, callerMemberName, callerLineNumber, data), LogType.Message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Message_NoCallerPrefix(object data)
        {
            LogSource.Log(data, LogType.Message);
        }

        internal static void Warning(object data, [CallerFilePath] string callerPath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = -1)
        {
            LogSource.Log(buildCallerLogString(callerPath, callerMemberName, callerLineNumber, data), LogType.Warning);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Warning_NoCallerPrefix(object data)
        {
            LogSource.Log(data, LogType.Warning);
        }
    }
}
