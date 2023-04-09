using RoR2;

namespace RiskOfChaos.EffectHandling.Controllers.ChatVoting
{
    public readonly struct EffectVoteHolder
    {
        public static readonly EffectVoteHolder Random = new EffectVoteHolder(default, true);

        public readonly ChaosEffectInfo EffectInfo;
        public readonly bool IsRandom;

        EffectVoteHolder(ChaosEffectInfo effectInfo, bool isRandom)
        {
            EffectInfo = effectInfo;
            IsRandom = isRandom;
        }

        public EffectVoteHolder(ChaosEffectInfo effectInfo) : this(effectInfo, false)
        {
        }

        public override readonly string ToString()
        {
            if (IsRandom)
            {
                return Language.GetString("CHAOS_EFFECT_VOTING_RANDOM_OPTION_NAME");
            }
            else
            {
                return EffectInfo.DisplayName;
            }
        }
    }
}
