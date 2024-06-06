namespace RiskOfTwitch.Logging
{
    public interface ILogSource
    {
        void Log(object message, LogType type);
    }
}
