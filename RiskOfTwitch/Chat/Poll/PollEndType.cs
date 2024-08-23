namespace RiskOfTwitch.Chat.Poll
{
    public enum PollEndType
    {
        /// <summary>
        /// Ends the poll before the poll is scheduled to end. The poll remains publicly visible.
        /// </summary>
        Terminate,
        /// <summary>
        /// Ends the poll before the poll is scheduled to end, and then archives it so it's no longer publicly visible.
        /// </summary>
        Archive
    }
}
