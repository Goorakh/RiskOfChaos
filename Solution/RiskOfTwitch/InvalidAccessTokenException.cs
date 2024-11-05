using System;

namespace RiskOfTwitch
{
    public sealed class InvalidAccessTokenException : Exception
    {
        public InvalidAccessTokenException() : base("The provided access token is not valid or has expired")
        {
        }
    }
}
