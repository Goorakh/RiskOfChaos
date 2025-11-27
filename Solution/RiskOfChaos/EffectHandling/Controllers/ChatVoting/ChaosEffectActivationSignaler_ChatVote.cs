using HG;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectDefinitions;
using RiskOfChaos.SaveHandling;
using RiskOfChaos.SaveHandling.DataContainers.EffectHandlerControllers;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace RiskOfChaos.EffectHandling.Controllers.ChatVoting
{
    public abstract class ChaosEffectActivationSignaler_ChatVote : ChaosEffectActivationSignaler
    {
        public delegate void OnEffectVotingFinishedDelegate(in EffectVoteResult result);
        public static event OnEffectVotingFinishedDelegate OnEffectVotingFinishedServer;

        static int numVoteOptions => Configs.ChatVoting.NumEffectOptions.Value + (Configs.ChatVoting.IncludeRandomEffectInVote.Value ? 1 : 0);

        public event Action OnVoteOptionsChanged;

        protected abstract Configs.ChatVoting.ChatVotingMode votingMode { get; }

        protected UniqueVoteSelection<string, EffectVoteInfo> _effectVoteSelection;

        bool _voteOptionsDirty = false;

        CompletePeriodicRunTimer _voteTimer;

        [SerializedMember("rng")]
        Xoroshiro128Plus _rng;

        [SerializedMember("lvet")]
        float serialized_lastVoteEndTimeStopwatch
        {
            get
            {
                if (!enabled)
                    return 0f;

                if (_voteTimer == null)
                    return 0f;

                return _voteTimer.GetLastActivationTimeStopwatch().Time;
            }
            set
            {
                if (!enabled)
                    return;

                if (_voteTimer == null)
                {
                    Log.Error("Failed to set last activation time, no timer instance");
                    return;
                }

                _voteTimer.SetLastActivationTimeStopwatch(value);

                Log.Debug($"Loaded timer data, remaining={_voteTimer.GetNextActivationTime().TimeUntil}");
            }
        }

        [SerializedMember("vd")]
        EffectActivationSignalerChatVoteData serialized_voteData
        {
            get
            {
                if (!enabled)
                    return null;

                EffectActivationSignalerChatVoteData voteData = new EffectActivationSignalerChatVoteData
                {
                    VotingMode = votingMode,
                    VotesStartedCount = _voteStartCount
                };

                EffectVoteInfo[] voteOptions = _effectVoteSelection.GetVoteOptions();
                SerializedEffectVoteInfo[] serializedVotes = new SerializedEffectVoteInfo[voteOptions.Length];

                for (int i = 0; i < voteOptions.Length; i++)
                {
                    serializedVotes[i] = new SerializedEffectVoteInfo
                    {
                        UserVotes = [.. _effectVoteSelection.GetVoteKeys(i)],
                        Effect = voteOptions[i].EffectInfo?.EffectIndex ?? ChaosEffectIndex.Invalid,
                        IsRandom = voteOptions[i].IsRandom
                    };
                }

                voteData.VoteSelection = serializedVotes;

                return voteData;
            }
            set
            {
                if (!enabled)
                    return;

                _voteStartCount = value.VotesStartedCount;

                SerializedEffectVoteInfo[] serializedVotes = value.VoteSelection;
                EffectVoteInfo[] effectVotes = new EffectVoteInfo[serializedVotes.Length];
                for (int i = 0; i < serializedVotes.Length; i++)
                {
                    int voteNumber = getVoteNumber(i);

                    if (serializedVotes[i].IsRandom)
                    {
                        effectVotes[i] = EffectVoteInfo.Random(voteNumber);
                    }
                    else
                    {
                        effectVotes[i] = new EffectVoteInfo(ChaosEffectCatalog.GetEffectInfo(serializedVotes[i].Effect), voteNumber);
                    }
                }

                if (effectVotes.Length > 0)
                {
                    startVote(effectVotes);

                    for (int i = 0; i < serializedVotes.Length; i++)
                    {
                        foreach (string userId in serializedVotes[i].UserVotes)
                        {
                            try
                            {
                                _effectVoteSelection.SetVote(userId, i);
                            }
                            catch (Exception e)
                            {
                                Log.Error_NoCallerPrefix($"Failed to set vote index {i} from user {userId}: {e}");
                            }
                        }
                    }

                    _voteOptionsDirty = true;
                }
            }
        }

        int _voteStartCount;

        bool offsetVoteNumbers => _voteStartCount % 2 == 0;

        public bool IsVoteActive => _effectVoteSelection != null && _effectVoteSelection.IsVoteActive;

        public int TotalVotes => _effectVoteSelection?.TotalVotes ?? 0;

        int getVoteNumber(int voteIndex)
        {
            if (offsetVoteNumbers)
            {
                voteIndex += numVoteOptions;
            }

            // 0-indexed to 1-indexed
            return voteIndex + 1;
        }

        int getVoteIndex(int voteNumber)
        {
            if (offsetVoteNumbers)
            {
                voteNumber -= _effectVoteSelection.NumOptions;
            }

            // 1-indexed to 0-indexed
            return voteNumber - 1;
        }

        public override void SkipAllScheduledEffects()
        {
            _voteTimer?.SkipAllScheduledActivations();
        }

        public override void RewindEffectScheduling(float numSeconds)
        {
            _voteTimer?.RewindScheduledActivations(numSeconds);
        }

        bool tryGetVoteNumber(string message, out int voteNumber)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                voteNumber = -1;
                return false;
            }

            CultureInfo cultureInfo = CultureInfo.InvariantCulture;

            if (int.TryParse(message, NumberStyles.Integer, cultureInfo, out voteNumber))
                return true;

            const string VOTE_PREFIX = "!vote";
            if (message.StartsWith(VOTE_PREFIX, true, cultureInfo))
            {
                if (int.TryParse(message.Remove(0, VOTE_PREFIX.Length), NumberStyles.Integer, cultureInfo, out voteNumber))
                    return true;
            }

            return false;
        }

        protected void processVoteMessage(string userId, string message)
        {
            if (!_effectVoteSelection.IsVoteActive)
                return;

            if (string.IsNullOrWhiteSpace(userId))
                return;

            if (tryGetVoteNumber(message, out int voteNumber))
            {
                int voteOptionIndex = getVoteIndex(voteNumber);

                if (_effectVoteSelection.IsValidOptionIndex(voteOptionIndex))
                {
                    Log.Debug($"Received vote {voteOptionIndex} from user {userId}");

                    _effectVoteSelection.SetVote(userId, voteOptionIndex);

                    _voteOptionsDirty = true;
                }
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (Run.instance)
            {
                _rng = new Xoroshiro128Plus(Run.instance.seed);
            }

            _effectVoteSelection = new UniqueVoteSelection<string, EffectVoteInfo>(numVoteOptions)
            {
                WinnerSelectionMode = Configs.ChatVoting.WinnerSelectionMode.Value
            };

            _voteTimer = new CompletePeriodicRunTimer(Configs.General.TimeBetweenEffects.Value);
            _voteTimer.OnActivate += onVoteEnd;

            Configs.General.TimeBetweenEffects.SettingChanged += onTimeBetweenEffectsChanged;
            Configs.General.DisableEffectDispatching.SettingChanged += onDisableEffectDispatchingChanged;
            Configs.ChatVoting.WinnerSelectionMode.SettingChanged += onVoteWinnerSelectionModeChanged;
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            Configs.General.TimeBetweenEffects.SettingChanged -= onTimeBetweenEffectsChanged;
            Configs.General.DisableEffectDispatching.SettingChanged -= onDisableEffectDispatchingChanged;
            Configs.ChatVoting.WinnerSelectionMode.SettingChanged -= onVoteWinnerSelectionModeChanged;

            if (_voteTimer != null)
            {
                _voteTimer.OnActivate -= onVoteEnd;
                _voteTimer = null;
            }

            endVote();

            _rng = null;
        }

        void onDisableEffectDispatchingChanged(object s, ConfigChangedArgs<bool> args)
        {
            if (args.NewValue)
            {
                endVote();
            }
            else
            {
                if (!_effectVoteSelection.IsVoteActive)
                {
                    beginNextVote();
                }
            }
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
                            effectVoteInfo.UpdateVotes(voteOption.NumVotes, totalVotes);
                        }
                    }
                }

                _voteOptionsDirty = false;
            }

            if (CanDispatchEffects)
            {
                _voteTimer.Update();
            }
        }

        void onTimeBetweenEffectsChanged(object s, ConfigChangedArgs<float> args)
        {
            if (_voteTimer == null)
                return;

            _voteTimer.Period = args.NewValue;
        }

        void onVoteWinnerSelectionModeChanged(object sender, ConfigChangedArgs<VoteWinnerSelectionMode> e)
        {
            if (_effectVoteSelection == null)
                return;

            _effectVoteSelection.WinnerSelectionMode = e.NewValue;
        }

        void beginNextVote()
        {
            if (Configs.General.DisableEffectDispatching.Value)
                return;

            _voteStartCount++;

            int numOptions = numVoteOptions;

            WeightedSelection<ChaosEffectInfo> effectSelection = ChaosEffectCatalog.GetAllActivatableEffects(new EffectCanActivateContext(_voteTimer.GetNextActivationTime()));

            EffectVoteInfo[] voteOptions = new EffectVoteInfo[numOptions];
            for (int i = 0; i < numOptions; i++)
            {
                int voteNumber = getVoteNumber(i);

                EffectVoteInfo voteInfo = new EffectVoteInfo(Nothing.EffectInfo, voteNumber);
                if (Configs.ChatVoting.IncludeRandomEffectInVote.Value && i == numOptions - 1)
                {
                    voteInfo = EffectVoteInfo.Random(voteNumber);
                }
                else
                {
                    if (effectSelection.Count > 0)
                    {
                        voteInfo = new EffectVoteInfo(effectSelection.GetAndRemoveRandom(_rng), voteNumber);
                    }
                    else
                    {
                        Log.Warning($"No activatable effects remain for vote {voteNumber}");
                    }
                }

                voteOptions[i] = voteInfo;
            }

            startVote(voteOptions);
        }

        void startVote(EffectVoteInfo[] voteOptions)
        {
            _effectVoteSelection.NumOptions = voteOptions.Length;
            _effectVoteSelection.StartVote(voteOptions);

            OnVoteOptionsChanged?.Invoke();
        }

        void endVote()
        {
            if (_effectVoteSelection == null || !_effectVoteSelection.IsVoteActive)
                return;

            _effectVoteSelection.EndVote();
            OnVoteOptionsChanged?.Invoke();
        }

        void onVoteEnd(RunTimeStamp activationTime)
        {
            if (_effectVoteSelection.IsVoteActive)
            {
                if (_effectVoteSelection.TryGetVoteResult(out EffectVoteInfo voteResult))
                {
                    OnEffectVotingFinishedServer?.Invoke(new EffectVoteResult(_effectVoteSelection, voteResult));
                    startEffect(voteResult, activationTime);
                }
                else
                {
                    Log.Warning("Failed to get vote result");
                }
            }

            endVote();

            beginNextVote();
        }

        void startEffect(EffectVoteInfo voteResult, RunTimeStamp activationTime)
        {
            ChaosEffectInfo effectInfo;
            ChaosEffectDispatchArgs dispatchArgs;
            if (voteResult.IsRandom)
            {
                EffectVoteInfo[] voteOptions = _effectVoteSelection.GetVoteOptions();
                using (SetPool<ChaosEffectInfo>.RentCollection(out HashSet<ChaosEffectInfo> voteOptionEffects))
                {
                    voteOptionEffects.EnsureCapacity(voteOptions.Length);
                    foreach (EffectVoteInfo voteInfo in voteOptions)
                    {
                        if (voteInfo.EffectInfo != null)
                        {
                            voteOptionEffects.Add(voteInfo.EffectInfo);
                        }
                    }

                    effectInfo = ChaosEffectCatalog.PickActivatableEffect(_rng, EffectCanActivateContext.Now, voteOptionEffects);
                }

                dispatchArgs = new ChaosEffectDispatchArgs
                {
                    DispatchFlags = EffectDispatchFlags.None
                };
            }
            else
            {
                effectInfo = voteResult.EffectInfo;

                dispatchArgs = new ChaosEffectDispatchArgs
                {
                    DispatchFlags = EffectDispatchFlags.CheckCanActivate
                };
            }

            dispatchArgs.OverrideStartTime = activationTime + Mathf.Round(activationTime.TimeSinceClamped);

            signalEffectDispatch(effectInfo, dispatchArgs);
        }

        public EffectVoteInfo[] GetCurrentVoteOptions()
        {
            if (_effectVoteSelection == null || !_effectVoteSelection.IsVoteActive)
                return [];

            return _effectVoteSelection.GetVoteOptions();
        }

        public override RunTimeStamp GetNextEffectActivationTime()
        {
            return _voteTimer.GetNextActivationTime();
        }
    }
}
