using RiskOfTwitch.Logging;
using System.Runtime.CompilerServices;

namespace RiskOfTwitch
{
    public static class Log
    {
        public static ILogSource LogSource { get; set; } = new ConsoleLogSource();

        static string getLogPrefix(string callerPath, string callerMemberName, int callerLineNumber)
        {
            const string PATH_NAME = "RiskOfTwitch";

            int nameLastPathIndex = callerPath.LastIndexOf(PATH_NAME);
            if (nameLastPathIndex >= 0)
            {
                callerPath = callerPath.Substring(nameLastPathIndex);
            }

            return $"{callerPath}:{callerLineNumber} ({callerMemberName}) ";
        }

#if DEBUG
        internal static void Debug(object data, [CallerFilePath] string callerPath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = -1) => LogSource.Log(getLogPrefix(callerPath, callerMemberName, callerLineNumber) + data, LogType.Debug);
        internal static void Debug_NoCallerPrefix(object data) => LogSource.Log(data, LogType.Debug);
#endif

        internal static void Error(object data, [CallerFilePath] string callerPath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = -1) => LogSource.Log(getLogPrefix(callerPath, callerMemberName, callerLineNumber) + data, LogType.Error);
        internal static void Error_NoCallerPrefix(object data) => LogSource.Log(data, LogType.Error);

        internal static void Fatal(object data, [CallerFilePath] string callerPath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = -1) => LogSource.Log(getLogPrefix(callerPath, callerMemberName, callerLineNumber) + data, LogType.Fatal);
        internal static void Fatal_NoCallerPrefix(object data) => LogSource.Log(data, LogType.Fatal);

        internal static void Info(object data, [CallerFilePath] string callerPath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = -1) => LogSource.Log(getLogPrefix(callerPath, callerMemberName, callerLineNumber) + data, LogType.Info);
        internal static void Info_NoCallerPrefix(object data) => LogSource.Log(data, LogType.Info);

        internal static void Message(object data, [CallerFilePath] string callerPath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = -1) => LogSource.Log(getLogPrefix(callerPath, callerMemberName, callerLineNumber) + data, LogType.Message);
        internal static void Message_NoCallerPrefix(object data) => LogSource.Log(data, LogType.Message);

        internal static void Warning(object data, [CallerFilePath] string callerPath = "", [CallerMemberName] string callerMemberName = "", [CallerLineNumber] int callerLineNumber = -1) => LogSource.Log(getLogPrefix(callerPath, callerMemberName, callerLineNumber) + data, LogType.Warning);
        internal static void Warning_NoCallerPrefix(object data) => LogSource.Log(data, LogType.Warning);
    }
}
