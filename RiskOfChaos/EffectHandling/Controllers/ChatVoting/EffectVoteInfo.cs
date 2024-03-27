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

        public uint Version { get; private set; }

        int _voteCount;
        public int VoteCount
        {
            get
            {
                return _voteCount;
            }
            set
            {
                if (_voteCount == value)
                    return;

                _voteCount = value;
                Version++;
            }
        }

        float _votePercentage;
        public float VotePercentage
        {
            get
            {
                return _votePercentage;
            }
            set
            {
                if (_votePercentage == value)
                    return;

                _votePercentage = value;
                Version++;
            }
        }

        EffectVoteInfo(ChaosEffectInfo effectInfo, bool isRandom, int voteNumber)
        {
            EffectInfo = effectInfo;
            IsRandom = isRandom;
            VoteNumber = voteNumber;
        }

        public EffectVoteInfo(ChaosEffectInfo effectInfo, int voteNumber) : this(effectInfo, false, voteNumber)
        {
        }

        public object[] GetArgs()
        {
            return [
                VoteNumber,
                IsRandom ? Language.GetString("CHAOS_EFFECT_VOTING_RANDOM_OPTION_NAME") : EffectInfo.GetLocalDisplayName(),
                VotePercentage * 100f
            ];
        }
    }
}
