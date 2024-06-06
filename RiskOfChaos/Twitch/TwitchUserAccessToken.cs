namespace RiskOfChaos.Twitch
{
    public readonly record struct TwitchUserAccessToken(string Scopes, string Token)
    {
        public static TwitchUserAccessToken Empty { get; } = new TwitchUserAccessToken(string.Empty, string.Empty);

        public bool IsEmpty => string.IsNullOrEmpty(Scopes) && string.IsNullOrEmpty(Token);
    }
}
