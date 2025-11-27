using System.Collections.Generic;
using System.Linq;

namespace RiskOfChaos.EffectHandling.Controllers.ChatVoting
{
    public sealed class UniqueVoteSelection<TVoteKey, TVoteResult> : VoteSelection<TVoteResult>
    {
        readonly Dictionary<TVoteKey, int> _voteByKey = [];

        public UniqueVoteSelection(int numOptions) : base(numOptions)
        {
        }

        public IEnumerable<TVoteKey> GetVoteKeys(int voteIndex)
        {
            return _voteByKey.Where(kvp => kvp.Value == voteIndex).Select(kvp => kvp.Key).Distinct();
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
