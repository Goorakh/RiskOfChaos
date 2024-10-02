namespace RiskOfChaos.EffectHandling.Controllers.ChatVoting
{
    public readonly struct EffectVoteResult
    {
        public readonly UniqueVoteSelection<string, EffectVoteInfo> VoteSelection;
        public readonly EffectVoteInfo WinningOption;

        public EffectVoteResult(UniqueVoteSelection<string, EffectVoteInfo> effectVoteSelection, EffectVoteInfo voteResult)
        {
            VoteSelection = effectVoteSelection;
            WinningOption = voteResult;
        }
    }
}
