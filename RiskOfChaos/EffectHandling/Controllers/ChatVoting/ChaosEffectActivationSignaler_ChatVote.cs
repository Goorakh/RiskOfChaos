using RoR2;
using UnityEngine;

namespace RiskOfChaos.EffectHandling.Controllers.ChatVoting
{
    public class ChaosEffectActivationSignaler_ChatVote : MonoBehaviour, IChaosEffectActivationSignaler
    {
        public event IChaosEffectActivationSignaler.SignalShouldDispatchEffectDelegate SignalShouldDispatchEffect;

        static int numVoteOptions
        {
            get
            {
                return Configs.ChatVoting.NumEffectOptions + (Configs.ChatVoting.IncludeRandomEffectInVote ? 1 : 0);
            }
        }

        readonly UniqueVoteSelection<string, EffectVoteHolder> _effectVoteSelection = new UniqueVoteSelection<string, EffectVoteHolder>(numVoteOptions);

        Xoroshiro128Plus _rng;

        public void SkipAllScheduledEffects()
        {
        }

        protected virtual void OnEnable()
        {
            if (Run.instance)
            {
                _rng = new Xoroshiro128Plus(Run.instance.runRNG.nextUlong);

                beginNextVote();
            }
        }

        protected virtual void OnDisable()
        {
            _rng = null;
            _effectVoteSelection.EndVote();
        }

        void beginNextVote()
        {
            int numOptions = numVoteOptions;
            _effectVoteSelection.NumOptions = numOptions;

            EffectVoteHolder[] voteOptions = new EffectVoteHolder[numOptions];
            for (int i = 0; i < numOptions; i++)
            {
                if (Configs.ChatVoting.IncludeRandomEffectInVote && i == numOptions - 1)
                {
                    voteOptions[i] = EffectVoteHolder.Random;
                }
                else
                {
                    voteOptions[i] = new EffectVoteHolder(ChaosEffectCatalog.PickActivatableEffect(_rng));
                }
            }

            _effectVoteSelection.StartVote(voteOptions);
        }

        protected void onChatMessageReceived(string userId, string message)
        {
            if (_effectVoteSelection.IsVoteActive &&
                int.TryParse(message, out int voteOptionIndex))
            {
                // 1-indexed to 0-indexed
                voteOptionIndex--;

#if DEBUG
                Log.Debug($"Received vote {voteOptionIndex} from user {userId}");
#endif

                if (_effectVoteSelection.IsValidOptionIndex(voteOptionIndex))
                {
                    _effectVoteSelection.SetVote(userId, voteOptionIndex);
                }
            }
        }
    }
}
