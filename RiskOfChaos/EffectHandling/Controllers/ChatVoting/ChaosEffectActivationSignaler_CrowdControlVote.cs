using System;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling.Controllers.ChatVoting
{
    public abstract class ChaosEffectActivationSignaler_CrowdControlVote : ChaosEffectActivationSignaler
    {
        public delegate void OnEffectVotingFinishedDelegate(in EffectVoteResult result);
        public static event OnEffectVotingFinishedDelegate OnEffectVotingFinishedServer;

        protected static void onEffectVotingFinishedServer(in EffectVoteResult result)
        {
            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return;
            }

            OnEffectVotingFinishedServer?.Invoke(result);
        }

        public virtual int NumVoteOptions => Configs.ChatVoting.NumEffectOptions.Value + (Configs.ChatVoting.IncludeRandomEffectInVote.Value ? 1 : 0);

        protected abstract Configs.ChatVoting.ChatVotingMode votingMode { get; }
    }
}
