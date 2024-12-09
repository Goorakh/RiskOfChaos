﻿using BepInEx.Logging;
using System.Diagnostics;

namespace RiskOfChaosPatcher
{
    class LogWriter
    {
        ManualLogSource _logSource;

        public LogWriter(ManualLogSource logSource)
        {
            _logSource = logSource;
        }

        public LogWriter() : this(null)
        {
        }

        public void SetLogSource(ManualLogSource logSource)
        {
            _logSource = logSource;
        }

        public void Fatal(object message)
        {
            _logSource?.LogFatal(message);
        }

        public void Error(object message)
        {
            _logSource?.LogError(message);
        }

        public void Warning(object message)
        {
            _logSource?.LogWarning(message);
        }

        public void Message(object message)
        {
            _logSource?.LogMessage(message);
        }

        public void Info(object message)
        {
            _logSource?.LogInfo(message);
        }

        [Conditional("DEBUG")]
        public void Debug(object message)
        {
            _logSource?.LogDebug(message);
        }
    }
}
