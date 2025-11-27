using System;

namespace RiskOfTwitch.EventSub
{
    public enum ConnectionErrorType
    {
        FailedRetrieveUser,
        FailedEventSubscribe,
        TokenAuthenticationFailed,
        TokenInvalid,
        Generic,
    }

    public sealed class ConnectionErrorEventArgs : EventArgs
    {
        public ConnectionErrorType Type { get; }

        public ConnectionErrorEventArgs(ConnectionErrorType type)
        {
            Type = type;
        }
    }
}
