using RiskOfChaos.EffectDefinitions;
using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfChaos.EffectHandling.Controllers.ChatVoting
{
    public class ChaosEffectActivationSignaler_ChatVote : ChaosEffectActivationSignaler
    {
        static int numVoteOptions
        {
            get
            {
                return Configs.ChatVoting.NumEffectOptions + (Configs.ChatVoting.IncludeRandomEffectInVote ? 1 : 0);
            }
        }

        public override event SignalShouldDispatchEffectDelegate SignalShouldDispatchEffect;

        public event Action OnVotingStarted;

        protected readonly UniqueVoteSelection<string, EffectVoteHolder> _effectVoteSelection = new UniqueVoteSelection<string, EffectVoteHolder>(numVoteOptions)
        {
            WinnerSelectionMode = Configs.ChatVoting.WinnerSelectionMode
        };

        CompletePeriodicRunTimer _voteTimer;

        Xoroshiro128Plus _rng;

        public override void SkipAllScheduledEffects()
        {
            _voteTimer?.SkipAllScheduledActivations();
        }

        protected void onChatMessageReceived(string userId, string message)
        {
            if (_effectVoteSelection.IsVoteActive &&
                int.TryParse(message, out int voteOptionIndex))
            {
                // 1-indexed to 0-indexed
                voteOptionIndex--;

                if (_effectVoteSelection.IsValidOptionIndex(voteOptionIndex))
                {
#if DEBUG
                    Log.Debug($"Received vote {voteOptionIndex} from user {userId}");
#endif

                    _effectVoteSelection.SetVote(userId, voteOptionIndex);
                }
            }
        }

        protected virtual void OnEnable()
        {
            if (Run.instance)
            {
                _rng = new Xoroshiro128Plus(Run.instance.runRNG.nextUlong);
            }

            Configs.General.OnTimeBetweenEffectsChanged += onTimeBetweenEffectsChanged;
            Configs.ChatVoting.OnWinnerSelectionModeChanged += onVoteWinnerSelectionModeChanged;

            _voteTimer = new CompletePeriodicRunTimer(Configs.General.TimeBetweenEffects);
            _voteTimer.OnActivate += onVoteEnd;
        }

        void Update()
        {
            if (!canDispatchEffects)
                return;

            _voteTimer.Update();
        }

        protected virtual void OnDisable()
        {
            Configs.General.OnTimeBetweenEffectsChanged -= onTimeBetweenEffectsChanged;
            Configs.ChatVoting.OnWinnerSelectionModeChanged -= onVoteWinnerSelectionModeChanged;

            if (_voteTimer != null)
            {
                _voteTimer.OnActivate -= onVoteEnd;
                _voteTimer = null;
            }

            _rng = null;
            _effectVoteSelection.EndVote();
        }

        void onTimeBetweenEffectsChanged()
        {
            if (_voteTimer == null)
                return;
            
            _voteTimer.Period = Configs.General.TimeBetweenEffects;
        }

        void onVoteWinnerSelectionModeChanged()
        {
            if (_effectVoteSelection == null)
                return;

            _effectVoteSelection.WinnerSelectionMode = Configs.ChatVoting.WinnerSelectionMode;
        }

        void beginNextVote()
        {
            int numOptions = numVoteOptions;
            _effectVoteSelection.NumOptions = numOptions;

            EffectCanActivateContext effectCanActivateContext = new EffectCanActivateContext(_voteTimer.GetTimeRemaining());
            HashSet<ChaosEffectInfo> usedEffects = new HashSet<ChaosEffectInfo>();

            EffectVoteHolder[] voteOptions = new EffectVoteHolder[numOptions];
            for (int i = 0; i < numOptions; i++)
            {
                if (Configs.ChatVoting.IncludeRandomEffectInVote && i == numOptions - 1)
                {
                    voteOptions[i] = EffectVoteHolder.Random;
                }
                else
                {
                    ChaosEffectInfo effectInfo = ChaosEffectCatalog.PickActivatableEffect(_rng, effectCanActivateContext, usedEffects);

                    if (usedEffects.Add(effectInfo))
                    {
                        voteOptions[i] = new EffectVoteHolder(effectInfo);
                    }
                    else
                    {
                        Log.Error($"Effect {effectInfo} is already used!");

                        voteOptions[i] = EffectVoteHolder.Random;
                    }
                }

#if DEBUG
                Log.Debug($"{i + 1}: {voteOptions[i]}");
#endif
            }

            _effectVoteSelection.StartVote(voteOptions);

            OnVotingStarted?.Invoke();
        }

        void onVoteEnd()
        {
            if (_effectVoteSelection.IsVoteActive)
            {
                if (_effectVoteSelection.TryGetVoteResult(out EffectVoteHolder voteResult))
                {
                    startEffect(voteResult);
                }
                else
                {
                    Log.Warning("Failed to get vote result");
                }
            }
            else
            {
                SignalShouldDispatchEffect?.Invoke(ChaosEffectCatalog.PickActivatableEffect(_rng, EffectCanActivateContext.Now));
            }

            beginNextVote();
        }

        void startEffect(EffectVoteHolder voteResult)
        {
            ChaosEffectInfo effectInfo;
            EffectDispatchFlags dispatchFlags;
            if (voteResult.IsRandom)
            {
                effectInfo = ChaosEffectCatalog.PickActivatableEffect(_rng, EffectCanActivateContext.Now);
                dispatchFlags = EffectDispatchFlags.None;
            }
            else
            {
                effectInfo = voteResult.EffectInfo;
                dispatchFlags = EffectDispatchFlags.CheckCanActivate;
            }

            SignalShouldDispatchEffect?.Invoke(effectInfo, dispatchFlags);
        }
    }
}
