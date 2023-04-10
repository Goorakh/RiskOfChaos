using RoR2;

namespace RiskOfChaos.EffectHandling.Controllers.ChatVoting
{
    public class EffectVoteInfo
    {
        public static EffectVoteInfo Random(int voteNumber)
        {
            return new EffectVoteInfo(default, true, voteNumber);
        }

        public readonly ChaosEffectInfo EffectInfo;
        public readonly bool IsRandom;

        public readonly int VoteNumber;

        public int VoteCount;
        public float VotePercentage;

        EffectVoteInfo(ChaosEffectInfo effectInfo, bool isRandom, int voteNumber)
        {
            EffectInfo = effectInfo;
            IsRandom = isRandom;
            VoteNumber = voteNumber;
        }

        public EffectVoteInfo(ChaosEffectInfo effectInfo, int voteNumber) : this(effectInfo, false, voteNumber)
        {
        }

        public override string ToString()
        {
            string effectName;
            if (IsRandom)
            {
                effectName = Language.GetString("CHAOS_EFFECT_VOTING_RANDOM_OPTION_NAME");
            }
            else
            {
                effectName = EffectInfo.DisplayName;
            }

            return $"{VoteNumber}: {effectName} ({VotePercentage * 100f:F0}%)";
        }
    }
}
