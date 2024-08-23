using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectDefinitions;
using RiskOfChaos.Twitch;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RiskOfTwitch;
using RiskOfTwitch.Chat.Poll;
using RoR2;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace RiskOfChaos.EffectHandling.Controllers.ChatVoting.Twitch
{
    [ChaosEffectActivationSignaler(Configs.ChatVoting.ChatVotingMode.TwitchPolls)]
    public class ChaosEffectActivationSignaler_TwitchPollVote : ChaosEffectActivationSignaler_CrowdControlVote
    {
        static ChaosEffectActivationSignaler_TwitchPollVote _instance;
        public static ChaosEffectActivationSignaler_TwitchPollVote Instance => _instance;

        public override event SignalShouldDispatchEffectDelegate SignalShouldDispatchEffect;

        public override int NumVoteOptions => Mathf.Clamp(base.NumVoteOptions, CreatePollArgs.MIN_CHOICE_COUNT, CreatePollArgs.MAX_CHOICE_COUNT);

        protected override Configs.ChatVoting.ChatVotingMode votingMode { get; } = Configs.ChatVoting.ChatVotingMode.TwitchPolls;

        Xoroshiro128Plus _effectRNG;

        string _broadcasterId;
        CancellationTokenSource _currentGetBroadcasterIdCancellationTokenSource;

        CancellationTokenSource _objectDisabledTokenSource;

        PollData _activePoll;
        EffectVoteInfo[] _activePollVoteOptions;

        CompletePeriodicRunTimer _pollTimer;

        readonly ConcurrentQueue<ChatMessageBase> _messageQueue = [];

        public override void RewindEffectScheduling(float numSeconds)
        {
            _pollTimer.RewindScheduledActivations(numSeconds);
        }

        public override void SkipAllScheduledEffects()
        {
            _pollTimer.SkipAllScheduledActivations();
        }

        public override float GetTimeUntilNextEffect()
        {
            return Mathf.Max(0f, _pollTimer.GetTimeRemaining());
        }

        void OnEnable()
        {
            SingletonHelper.Assign(ref _instance, this);

            _effectRNG = new Xoroshiro128Plus(Run.instance ? Run.instance.stageRng : RoR2Application.rng);

            _pollTimer = new CompletePeriodicRunTimer(Configs.General.TimeBetweenEffects.Value, TimerFlags.EnforcePeriodOnTimerSwitch);
            _pollTimer.OnActivate += onTimer;

            // .Clear() doesn't exist in this version (:
            while (_messageQueue.TryDequeue(out _))
            {
            }

            _objectDisabledTokenSource = new CancellationTokenSource();

            Configs.General.TimeBetweenEffects.SettingChanged += TimeBetweenEffects_SettingChanged;

            TwitchAuthenticationManager.OnAccessTokenChanged += refreshBroadcasterId;

            Configs.ChatVoting.OnReconnectButtonPressed += refreshBroadcasterId;

            refreshBroadcasterId();
        }

        void OnDisable()
        {
            SingletonHelper.Unassign(ref _instance, this);

            Configs.General.TimeBetweenEffects.SettingChanged -= TimeBetweenEffects_SettingChanged;

            TwitchAuthenticationManager.OnAccessTokenChanged -= refreshBroadcasterId;

            Configs.ChatVoting.OnReconnectButtonPressed -= refreshBroadcasterId;

            if (_currentGetBroadcasterIdCancellationTokenSource != null)
            {
                _currentGetBroadcasterIdCancellationTokenSource.Cancel();
                _currentGetBroadcasterIdCancellationTokenSource.Dispose();
                _currentGetBroadcasterIdCancellationTokenSource = null;
            }

            if (!string.IsNullOrEmpty(_broadcasterId) && _activePoll != null)
            {
                forceEndCurrentPoll();
            }

            if (_objectDisabledTokenSource != null)
            {
                _objectDisabledTokenSource.Cancel();
                _objectDisabledTokenSource.Dispose();
                _objectDisabledTokenSource = null;
            }

            _activePoll = null;
            _activePollVoteOptions = null;
            _broadcasterId = null;
            _effectRNG = null;
            _pollTimer = null;
        }

        void Update()
        {
            if (canDispatchEffects)
            {
                _pollTimer.Update();
            }

            if (Run.FixedTimeStamp.now.t >= 1f && !PauseManager.isPaused)
            {
                while (_messageQueue.TryDequeue(out ChatMessageBase message))
                {
                    Chat.SendBroadcastChat(message);
                }
            }
        }

        void TimeBetweenEffects_SettingChanged(object sender, ConfigChangedArgs<float> e)
        {
            _pollTimer.Period = e.NewValue;
        }

        void refreshBroadcasterId()
        {
            if (_currentGetBroadcasterIdCancellationTokenSource != null)
            {
                _currentGetBroadcasterIdCancellationTokenSource.Cancel();
                _currentGetBroadcasterIdCancellationTokenSource.Dispose();
                _currentGetBroadcasterIdCancellationTokenSource = null;
            }

            _broadcasterId = null;
            _currentGetBroadcasterIdCancellationTokenSource = new CancellationTokenSource();

            CancellationToken cancellationToken = _currentGetBroadcasterIdCancellationTokenSource.Token;

            Task.Factory.StartNew(async () =>
            {
                if (TwitchAuthenticationManager.CurrentAccessToken.IsEmpty)
                {
                    _messageQueue.Enqueue(new Chat.SimpleChatMessage
                    {
                        baseToken = "TWITCH_EFFECT_VOTING_LOGIN_FAIL_FORMAT",
                        paramTokens = [Language.GetString("TWITCH_LOGIN_FAIL_NOT_LOGGED_IN")]
                    });

                    return;
                }

                string accessToken = TwitchAuthenticationManager.CurrentAccessToken.Token;

                string broadcasterId = null;
                string broadcasterName = null;

                AuthenticationTokenValidationResponse authenticationTokenValidation = await Authentication.GetAccessTokenValidationAsync(accessToken, cancellationToken);

                if (authenticationTokenValidation != null)
                {
                    broadcasterId = authenticationTokenValidation.UserID;
                    broadcasterName = authenticationTokenValidation.Username;
                }

                if (string.IsNullOrEmpty(broadcasterId))
                {
                    _messageQueue.Enqueue(new Chat.SimpleChatMessage
                    {
                        baseToken = "TWITCH_EFFECT_VOTING_LOGIN_FAIL_FORMAT",
                        paramTokens = [Language.GetString("TWITCH_EFFECT_VOTING_CLIENT_CONNECT_FAIL_USER_RETRIEVE_FAILED")]
                    });

                    return;
                }

                _messageQueue.Enqueue(new Chat.SimpleChatMessage
                {
                    baseToken = "TWITCH_EFFECT_VOTING_LOGIN_SUCCESS",
                    paramTokens = [broadcasterName]
                });

                _broadcasterId = broadcasterId;

                _currentGetBroadcasterIdCancellationTokenSource.Dispose();
                _currentGetBroadcasterIdCancellationTokenSource = null;
            }, cancellationToken, TaskCreationOptions.None, UnityUpdateTaskScheduler.Instance);
        }

        void onTimer()
        {
            if (TwitchAuthenticationManager.CurrentAccessToken.IsEmpty)
                return;

            List<CancellationToken> cancellationTokens = new List<CancellationToken>(2);
            if (_currentGetBroadcasterIdCancellationTokenSource != null)
                cancellationTokens.Add(_currentGetBroadcasterIdCancellationTokenSource.Token);

            if (_objectDisabledTokenSource != null)
                cancellationTokens.Add(_objectDisabledTokenSource.Token);

            CancellationTokenSource linkedTokenSource = cancellationTokens.Count > 0 ? CancellationTokenSource.CreateLinkedTokenSource(cancellationTokens.ToArray()) : null;
            CancellationToken cancellationToken = linkedTokenSource?.Token ?? default;
            Task<Task> task = Task.Factory.StartNew(async () =>
            {
                if (_activePoll != null)
                {
                    await endCurrentPoll(cancellationToken);
                }

                await startNextPoll(cancellationToken);
            }, cancellationToken, TaskCreationOptions.None, UnityUpdateTaskScheduler.Instance);

            task.ContinueWith((task, disposableState) =>
            {
                if (disposableState is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }, linkedTokenSource);
        }

        void forceEndCurrentPoll()
        {
            string pollId = _activePoll.PollID;
            if (string.IsNullOrWhiteSpace(pollId))
            {
                Log.Error("No active poll");
                return;
            }

            string broadcasterId = _broadcasterId;
            if (string.IsNullOrWhiteSpace(broadcasterId))
            {
                Log.Error("Not logged in");
                return;
            }

            Task.Factory.StartNew(() => forceEndPoll(pollId, broadcasterId), default, TaskCreationOptions.None, UnityUpdateTaskScheduler.Instance);

            _activePoll = null;

            static async Task forceEndPoll(string pollId, string broadcasterId, CancellationToken cancellationToken = default)
            {
                EndPollArgs endPollArgs = new EndPollArgs(TwitchAuthenticationManager.CurrentAccessToken.Token, broadcasterId, pollId, PollEndType.Terminate);
                Result<PollData> endPollResult = await StaticTwitchAPI.EndPoll(endPollArgs);
                if (endPollResult.IsError)
                {
                    Log.Error($"Error ending poll: {endPollResult.Exception}");
                }
            }
        }

        async Task endCurrentPoll(CancellationToken cancellationToken = default)
        {
            if (_activePoll == null)
            {
                Log.Error("No active poll");
                return;
            }

            PollData activePollData = null;

            GetPollsArgs getPollsArgs = new GetPollsArgs(TwitchAuthenticationManager.CurrentAccessToken.Token, _broadcasterId, [_activePoll.PollID]);
            Result<GetPollsResponse> getPollsResult = await StaticTwitchAPI.GetPolls(getPollsArgs, cancellationToken);
            if (getPollsResult.IsResult)
            {
                activePollData = getPollsResult.Value.Polls[0];

                if (activePollData.Status == "ACTIVE")
                {
                    EndPollArgs endPollArgs = new EndPollArgs(TwitchAuthenticationManager.CurrentAccessToken.Token, _broadcasterId, _activePoll.PollID, PollEndType.Terminate);
                    Result<PollData> endPollResult = await StaticTwitchAPI.EndPoll(endPollArgs, cancellationToken);
                    if (endPollResult.IsResult)
                    {
                        activePollData = endPollResult.Value;
                    }
                    else
                    {
                        Log.Error_NoCallerPrefix($"Error ending poll: {endPollResult.Exception}");
                    }
                }
            }
            else
            {
                Log.Error_NoCallerPrefix($"Error retrieving poll data: {getPollsResult.Exception}");
            }

            _activePoll = activePollData;
            if (_activePoll != null)
            {
                onPollEnded();
            }
            else
            {
                _messageQueue.Enqueue(new Chat.SimpleChatMessage
                {
                    baseToken = "",
                    paramTokens = []
                });
            }

            _activePoll = null;
        }

        void onPollEnded()
        {
            int pollChoiceCount = _activePoll.Choices.Length;
            if (_activePollVoteOptions == null || _activePollVoteOptions.Length != pollChoiceCount)
            {
                Log.Error("Active poll options do not match saved options");
                return;
            }

            VoteSelection<EffectVoteInfo> effectVoteSelection = new VoteSelection<EffectVoteInfo>(pollChoiceCount);
            effectVoteSelection.WinnerSelectionMode = VoteWinnerSelectionMode.MostVotes;
            effectVoteSelection.StartVote(_activePollVoteOptions);

            int totalVotes = 0;

            for (int i = 0; i < pollChoiceCount; i++)
            {
                int optionVoteCount = _activePoll.Choices[i].Votes;
                effectVoteSelection.SetVoteCount(i, optionVoteCount);

                totalVotes += optionVoteCount;
            }

            for (int i = 0; i < pollChoiceCount; i++)
            {
                if (effectVoteSelection.TryGetOption(i, out VoteSelection<EffectVoteInfo>.VoteOption voteOption))
                {
                    EffectVoteInfo effectVoteInfo = voteOption.Value;
                    effectVoteInfo.VoteCount = voteOption.NumVotes;
                    effectVoteInfo.VotePercentage = totalVotes > 0 ? effectVoteInfo.VoteCount / (float)totalVotes : 1f / pollChoiceCount;
                }
            }

            if (effectVoteSelection.TryGetVoteResult(out EffectVoteInfo voteResult))
            {
                onEffectVotingFinishedServer(new EffectVoteResult(effectVoteSelection, voteResult));

                ChaosEffectInfo effectInfo;
                ChaosEffectDispatchArgs dispatchArgs;
                if (voteResult.IsRandom)
                {
                    HashSet<ChaosEffectInfo> voteOptionEffects = new HashSet<ChaosEffectInfo>(effectVoteSelection.GetVoteOptions()
                                                                                                                 .Select(vote => vote.EffectInfo)
                                                                                                                 .Where(effect => effect != null));

                    effectInfo = ChaosEffectCatalog.PickActivatableEffect(_effectRNG.Branch(), EffectCanActivateContext.Now, voteOptionEffects);

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

                SignalShouldDispatchEffect?.Invoke(effectInfo, dispatchArgs);
            }
            else
            {
                Log.Error("Failed to get vote result");
            }
        }

        async Task startNextPoll(CancellationToken cancellationToken = default)
        {
            if (Configs.General.DisableEffectDispatching.Value)
                return;

            const int POLL_END_DURATION = 1;

            float timeUntilEffect = GetTimeUntilNextEffect();

            timeUntilEffect /= TimeUtils.UnpausedTimeScale;

            int pollDuration = Mathf.FloorToInt(timeUntilEffect);
            if (pollDuration >= CreatePollArgs.MIN_DURATION + POLL_END_DURATION)
            {
                pollDuration -= POLL_END_DURATION;
            }
            else if (pollDuration > CreatePollArgs.MIN_DURATION)
            {
                pollDuration = CreatePollArgs.MIN_DURATION;
            }

            if (pollDuration > CreatePollArgs.MAX_DURATION)
            {
                pollDuration = CreatePollArgs.MAX_DURATION;
            }

            int numChoices = NumVoteOptions;
            CreatePollChoiceArgs[] pollChoices = new CreatePollChoiceArgs[numChoices];
            Array.Resize(ref _activePollVoteOptions, numChoices);

            WeightedSelection<ChaosEffectInfo> selectableEffects = ChaosEffectCatalog.GetAllActivatableEffects(new EffectCanActivateContext(timeUntilEffect));

            for (int i = 0; i < numChoices; i++)
            {
                EffectVoteInfo effectVoteInfo;

                if (Configs.ChatVoting.IncludeRandomEffectInVote.Value && i == numChoices - 1)
                {
                    effectVoteInfo = EffectVoteInfo.Random(i);
                }
                else if (selectableEffects.Count > 0)
                {
                    effectVoteInfo = new EffectVoteInfo(selectableEffects.GetAndRemoveRandom(_effectRNG.Branch()), i);
                }
                else
                {
                    Log.Warning($"No effect available for vote {i}");
                    effectVoteInfo = new EffectVoteInfo(Nothing.EffectInfo, i);
                }

                _activePollVoteOptions[i] = effectVoteInfo;

                string pollChoiceTitle = effectVoteInfo.GetEffectName(EffectNameFormatFlags.RuntimeFormatArgs | EffectNameFormatFlags.Short);

                // Remove rich text tags
                pollChoiceTitle = Regex.Replace(pollChoiceTitle, @"<(?=\S)[^<>]+>", string.Empty);

                pollChoices[i] = new CreatePollChoiceArgs(pollChoiceTitle);
            }

            CreatePollArgs createPollArgs = new CreatePollArgs(TwitchAuthenticationManager.CurrentAccessToken.Token, _broadcasterId, Language.GetString("CHAOS_EFFECT_VOTING_TITLE"), pollChoices, pollDuration);

            if (Configs.TwitchVoting.AllowChannelPointPollVotes.Value)
            {
                createPollArgs.AllowChannelPointVoting = true;
                createPollArgs.ChannelPointsPerVote = (uint)Mathf.Clamp(Configs.TwitchVoting.ChannelPointsPerAdditionalVote.Value, CreatePollArgs.MIN_CHANNEL_POINTS_PER_VOTE, CreatePollArgs.MAX_CHANNEL_POINTS_PER_VOTE);
            }

            Result<PollData> createPollResult = await StaticTwitchAPI.CreatePoll(createPollArgs, cancellationToken);

            if (createPollResult.IsResult)
            {
                _activePoll = createPollResult.Value;
            }
            else
            {
                Log.Error_NoCallerPrefix(createPollResult.Exception);
            }
        }
    }
}
