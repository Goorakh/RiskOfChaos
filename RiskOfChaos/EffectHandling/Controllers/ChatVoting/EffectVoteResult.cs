namespace RiskOfChaos.EffectHandling.Controllers.ChatVoting
{
    public readonly struct EffectVoteResult
    {
        public readonly VoteSelection<EffectVoteInfo> VoteSelection;
        public readonly EffectVoteInfo WinningOption;

        public EffectVoteResult(VoteSelection<EffectVoteInfo> effectVoteSelection, EffectVoteInfo voteResult)
        {
            VoteSelection = effectVoteSelection;
            WinningOption = voteResult;
        }
    }
}
