using System.Collections.Generic;

namespace RiskOfChaos.EffectHandling.Controllers.ChatVoting
{
    public class UniqueVoteSelection<TVoteKey, TVoteResult> : VoteSelection<TVoteResult>
    {
        readonly Dictionary<TVoteKey, int> _voteByKey = new Dictionary<TVoteKey, int>();

        public UniqueVoteSelection(int numOptions) : base(numOptions)
        {
        }

        public void SetVote(TVoteKey key, int optionIndex)
        {
            checkOptionIndex(optionIndex);

            if (_voteByKey.TryGetValue(key, out int existingVoteIndex))
            {
                ref VoteOption existingVoteOption = ref getOption(existingVoteIndex);
                existingVoteOption.NumVotes--;
            }

            ref VoteOption newVoteOption = ref getOption(optionIndex);
            newVoteOption.NumVotes++;

            _voteByKey[key] = optionIndex;
        }

        protected override void resetState()
        {
            base.resetState();

            _voteByKey.Clear();
        }
    }
}
