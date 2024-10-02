using RiskOfChaos.ConfigHandling;
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

            Configs.ChatVoting.VotingMode.SettingChanged += VotingMode_SettingChanged;
        }

        ~ChaosEffectActivationSignalerAttribute()
        {
            Configs.ChatVoting.VotingMode.SettingChanged -= VotingMode_SettingChanged;
        }

        void VotingMode_SettingChanged(object sender, ConfigChangedArgs<Configs.ChatVoting.ChatVotingMode> e)
        {
            invokeShouldRefreshEnabledState();
        }

        public override bool CanBeActive()
        {
            return base.CanBeActive() && Configs.ChatVoting.VotingMode.Value == _requiredVotingMode;
        }
    }
}
