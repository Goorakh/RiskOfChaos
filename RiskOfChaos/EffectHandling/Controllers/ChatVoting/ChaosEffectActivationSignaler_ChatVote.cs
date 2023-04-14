using RiskOfChaos.UI;
using RiskOfChaos.UI.ChatVoting;
using RoR2;
using System;
using System.Collections.Generic;

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

        protected UniqueVoteSelection<string, EffectVoteInfo> _effectVoteSelection;

        bool _voteOptionsDirty = false;

        CompletePeriodicRunTimer _voteTimer;

        Xoroshiro128Plus _rng;

        public override void SkipAllScheduledEffects()
        {
            _voteTimer?.SkipAllScheduledActivations();
        }

        protected void processVoteMessage(string userId, string message)
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

                    _voteOptionsDirty = true;
                }
            }
        }

        protected virtual void OnEnable()
        {
            if (Run.instance)
            {
                _rng = new Xoroshiro128Plus(Run.instance.runRNG.nextUlong);
            }

            _effectVoteSelection = new UniqueVoteSelection<string, EffectVoteInfo>(numVoteOptions)
            {
                WinnerSelectionMode = Configs.ChatVoting.WinnerSelectionMode
            };

            _voteTimer = new CompletePeriodicRunTimer(Configs.General.TimeBetweenEffects);
            _voteTimer.OnActivate += onVoteEnd;

            Configs.General.OnTimeBetweenEffectsChanged += onTimeBetweenEffectsChanged;
            Configs.ChatVoting.OnWinnerSelectionModeChanged += onVoteWinnerSelectionModeChanged;

            ChaosEffectVoteDisplayController.OnDisplayControllerCreated += onEffectDisplayControllerCreated;
        }

        protected virtual void Update()
        {
            if (_voteOptionsDirty)
            {
                if (_effectVoteSelection.IsVoteActive)
                {
                    int totalVotes = _effectVoteSelection.TotalVotes;

                    int numVoteOptions = _effectVoteSelection.NumOptions;

                    for (int i = 0; i < numVoteOptions; i++)
                    {
                        if (_effectVoteSelection.TryGetOption(i, out VoteSelection<EffectVoteInfo>.VoteOption voteOption))
                        {
                            EffectVoteInfo effectVoteInfo = voteOption.Value;
                            effectVoteInfo.VoteCount = voteOption.NumVotes;
                            effectVoteInfo.VotePercentage = voteOption.NumVotes / (float)totalVotes;
                        }
                    }
                }

                _voteOptionsDirty = false;
            }

            if (_effectVoteSelection.IsVoteActive)
            {
                const float START_FADE_TIME = 2.5f;

                float voteTimeRemaining = _voteTimer.GetTimeRemaining();
                if (voteTimeRemaining <= START_FADE_TIME)
                {
                    if (ChaosUIController.Instance)
                    {
                        ChaosEffectVoteDisplayController effectVoteDisplayController = ChaosUIController.Instance.EffectVoteDisplayController;
                        if (effectVoteDisplayController)
                        {
                            effectVoteDisplayController.SetVoteDisplayAlpha(Util.Remap(voteTimeRemaining, 0f, START_FADE_TIME, 0f, 1f));
                        }
                    }
                }
            }

            if (canDispatchEffects)
            {
                _voteTimer.Update();
            }
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

            if (_effectVoteSelection != null)
            {
                _effectVoteSelection.EndVote();
                _effectVoteSelection = null;
            }

            _rng = null;

            if (ChaosUIController.Instance)
            {
                ChaosEffectVoteDisplayController effectVoteDisplayController = ChaosUIController.Instance.EffectVoteDisplayController;
                if (effectVoteDisplayController)
                {
                    effectVoteDisplayController.RemoveAllVoteDisplays();
                }
            }

            ChaosEffectVoteDisplayController.OnDisplayControllerCreated -= onEffectDisplayControllerCreated;
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

            EffectVoteInfo[] voteOptions = new EffectVoteInfo[numOptions];
            for (int i = 0; i < numOptions; i++)
            {
                int voteNumber = i + 1;

                if (Configs.ChatVoting.IncludeRandomEffectInVote && i == numOptions - 1)
                {
                    voteOptions[i] = EffectVoteInfo.Random(voteNumber);
                }
                else
                {
                    ChaosEffectInfo effectInfo = ChaosEffectCatalog.PickActivatableEffect(_rng, effectCanActivateContext, usedEffects);

                    if (usedEffects.Add(effectInfo))
                    {
                        voteOptions[i] = new EffectVoteInfo(effectInfo, voteNumber);
                    }
                    else
                    {
                        Log.Error($"Effect {effectInfo} is already used!");

                        voteOptions[i] = EffectVoteInfo.Random(voteNumber);
                    }
                }
            }

            _effectVoteSelection.StartVote(voteOptions);

            if (ChaosUIController.Instance)
            {
                ChaosEffectVoteDisplayController effectVoteDisplayController = ChaosUIController.Instance.EffectVoteDisplayController;
                if (effectVoteDisplayController)
                {
                    effectVoteDisplayController.DisplayVote(voteOptions);
                }
            }

            OnVotingStarted?.Invoke();
        }

        void onEffectDisplayControllerCreated(ChaosEffectVoteDisplayController effectVoteDisplayController)
        {
            if (_effectVoteSelection.IsVoteActive)
            {
                effectVoteDisplayController.DisplayVote(_effectVoteSelection.GetVoteOptions());
            }
        }

        void onVoteEnd()
        {
            if (_effectVoteSelection.IsVoteActive)
            {
                if (_effectVoteSelection.TryGetVoteResult(out EffectVoteInfo voteResult))
                {
                    startEffect(voteResult);
                }
                else
                {
                    Log.Warning("Failed to get vote result");
                }
            }

            beginNextVote();
        }

        void startEffect(EffectVoteInfo voteResult)
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
