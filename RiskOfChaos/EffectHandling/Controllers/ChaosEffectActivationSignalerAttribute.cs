using System;

namespace RiskOfChaos.EffectHandling.Controllers
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ChaosEffectActivationSignalerAttribute : ChaosControllerAttribute
    {
        readonly Configs.ChatVoting.ChatVotingMode _requiredVotingMode;

        public ChaosEffectActivationSignalerAttribute(Configs.ChatVoting.ChatVotingMode requiredVotingMode) : base(true)
        {
            _requiredVotingMode = requiredVotingMode;
        }

        public override bool CanBeActive()
        {
            return base.CanBeActive() && Configs.ChatVoting.VotingMode == _requiredVotingMode;
        }
    }
}
