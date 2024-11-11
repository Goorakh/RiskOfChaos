using System;

namespace RiskOfChaos.EffectHandling.Controllers.ChatVoting
{
    public class EffectVoteInfo
    {
        public static EffectVoteInfo Random(int voteNumber)
        {
            return new EffectVoteInfo(null, voteNumber, true);
        }

        public readonly ChaosEffectInfo EffectInfo;
        public readonly bool IsRandom;

        public readonly int VoteNumber;

        public int VoteCount { get; private set; }

        public float VotePercentage { get; private set; }

        public event Action OnVotesChanged;

        EffectVoteInfo(ChaosEffectInfo effectInfo, int voteNumber, bool isRandom)
        {
            EffectInfo = effectInfo;
            IsRandom = isRandom;
            VoteNumber = voteNumber;
        }

        public EffectVoteInfo(ChaosEffectInfo effectInfo, int voteNumber) : this(effectInfo, voteNumber, false)
        {
        }

        public void UpdateVotes(int optionVotes, int totalVotes)
        {
            bool votesChanged = false;

            votesChanged |= VoteCount != optionVotes;
            VoteCount = optionVotes;

            float votePercentage = 0f;
            if (totalVotes > 0)
            {
                votePercentage = VoteCount / (float)totalVotes;
            }

            votesChanged |= VotePercentage != votePercentage;
            VotePercentage = votePercentage;

            if (votesChanged)
            {
                OnVotesChanged?.Invoke();
            }
        }
    }
}
