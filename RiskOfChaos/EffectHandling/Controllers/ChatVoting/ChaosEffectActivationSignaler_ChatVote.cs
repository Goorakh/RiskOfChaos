using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectDefinitions;
using RiskOfChaos.SaveHandling;
using RiskOfChaos.SaveHandling.DataContainers;
using RiskOfChaos.SaveHandling.DataContainers.EffectHandlerControllers;
using RiskOfChaos.UI;
using RiskOfChaos.UI.ChatVoting;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RiskOfChaos.EffectHandling.Controllers.ChatVoting
{
    public abstract class ChaosEffectActivationSignaler_ChatVote : ChaosEffectActivationSignaler
    {
        public delegate void OnEffectVotingFinishedDelegate(in EffectVoteResult result);
        public static event OnEffectVotingFinishedDelegate OnEffectVotingFinishedServer;

        static int numVoteOptions => Configs.ChatVoting.NumEffectOptions.Value + (Configs.ChatVoting.IncludeRandomEffectInVote.Value ? 1 : 0);

        public override event SignalShouldDispatchEffectDelegate SignalShouldDispatchEffect;

        public event Action OnVotingStarted;

        protected abstract Configs.ChatVoting.ChatVotingMode votingMode { get; }

        protected UniqueVoteSelection<string, EffectVoteInfo> _effectVoteSelection;

        bool _voteOptionsDirty = false;

        CompletePeriodicRunTimer _voteTimer;

        Xoroshiro128Plus _rng;

        int _voteStartCount;
        bool _offsetVoteNumbers;

        int getVoteNumber(int voteIndex)
        {
            if (_offsetVoteNumbers)
            {
                voteIndex += numVoteOptions;
            }

            // 0-indexed to 1-indexed
            return voteIndex + 1;
        }

        int getVoteIndex(int voteNumber)
        {
            if (_offsetVoteNumbers)
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

        protected void processVoteMessage(string userId, string message)
        {
            if (_effectVoteSelection.IsVoteActive &&
                byte.TryParse(message, out byte voteNumber))
            {
                int voteOptionIndex = getVoteIndex(voteNumber);

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

            ChaosEffectVoteDisplayController.OnDisplayControllerCreated += onEffectDisplayControllerCreated;

            if (SaveManager.UseSaveData)
            {
                SaveManager.CollectSaveData += SaveManager_CollectSaveData;
                SaveManager.LoadSaveData += SaveManager_LoadSaveData;
            }
        }

        protected void SaveManager_CollectSaveData(ref SaveContainer container)
        {
            ref EffectActivationSignalerData data = ref container.ActivationSignalerData;
            if (data is not null || data.ChatVoteData is not null)
                return;

            data = new EffectActivationSignalerData
            {
                NextEffectRng = new SerializableRng(_rng),
                LastEffectActivationTime = _voteTimer.GetLastActivationTimeStopwatch(),
                ChatVoteData = new EffectActivationSignalerChatVoteData
                {
                    VotingMode = votingMode,
                    VotesStartedCount = _voteStartCount,
                    OffsetVoteNumbers = _offsetVoteNumbers
                }
            };

            EffectVoteInfo[] voteOptions = _effectVoteSelection.GetVoteOptions();
            SerializedEffectVoteInfo[] serializedVotes = new SerializedEffectVoteInfo[voteOptions.Length];

            for (int i = 0; i < voteOptions.Length; i++)
            {
                serializedVotes[i] = new SerializedEffectVoteInfo
                {
                    UserVotes = _effectVoteSelection.GetVoteKeys(i).ToArray(),
                    Effect = new SerializableEffect(voteOptions[i].EffectInfo),
                    IsRandom = voteOptions[i].IsRandom
                };
            }

            data.ChatVoteData.VoteSelection = serializedVotes;
        }

        protected void SaveManager_LoadSaveData(in SaveContainer container)
        {
            EffectActivationSignalerData data = container.ActivationSignalerData;
            if (data is null || data.ChatVoteData is null || data.ChatVoteData.VotingMode != votingMode)
                return;

            _rng = data.NextEffectRng;
            _voteTimer.SetLastActivationTimeStopwatch(data.LastEffectActivationTime);

            _voteStartCount = data.ChatVoteData.VotesStartedCount;
            _offsetVoteNumbers = data.ChatVoteData.OffsetVoteNumbers;

            SerializedEffectVoteInfo[] serializedVotes = data.ChatVoteData.VoteSelection;
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
                    effectVotes[i] = new EffectVoteInfo(serializedVotes[i].Effect.EffectInfo, voteNumber);
                }
            }

            _effectVoteSelection.NumOptions = effectVotes.Length;
            _effectVoteSelection.StartVote(effectVotes);

            for (int i = 0; i < serializedVotes.Length; i++)
            {
                foreach (string userId in serializedVotes[i].UserVotes)
                {
                    _effectVoteSelection.SetVote(userId, i);
                }
            }

            _voteOptionsDirty = true;

            if (ChaosUIController.Instance)
            {
                ChaosEffectVoteDisplayController effectVoteDisplayController = ChaosUIController.Instance.EffectVoteDisplayController;
                if (effectVoteDisplayController)
                {
                    effectVoteDisplayController.DisplayVote(effectVotes);
                }
            }

            OnVotingStarted?.Invoke();
        }

        void onDisableEffectDispatchingChanged(object s, ConfigChangedArgs<bool> args)
        {
            if (args.NewValue)
            {
                if (_effectVoteSelection.IsVoteActive)
                {
                    _effectVoteSelection.EndVote();

                    if (ChaosUIController.Instance)
                    {
                        ChaosEffectVoteDisplayController effectVoteDisplayController = ChaosUIController.Instance.EffectVoteDisplayController;
                        if (effectVoteDisplayController)
                        {
                            effectVoteDisplayController.RemoveAllVoteDisplays();
                        }
                    }
                }
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

                if (ChaosUIController.Instance)
                {
                    ChaosEffectVoteDisplayController effectVoteDisplayController = ChaosUIController.Instance.EffectVoteDisplayController;
                    if (effectVoteDisplayController)
                    {
                        float voteTimeRemaining = _voteTimer.GetTimeRemaining();
                        if (voteTimeRemaining <= START_FADE_TIME && canDispatchEffects)
                        {
                            effectVoteDisplayController.SetVoteDisplayAlpha(Util.Remap(voteTimeRemaining, 0f, START_FADE_TIME, 0f, 1f));
                        }
                        else
                        {
                            effectVoteDisplayController.SetVoteDisplayAlpha(1f);
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
            Configs.General.TimeBetweenEffects.SettingChanged -= onTimeBetweenEffectsChanged;
            Configs.General.DisableEffectDispatching.SettingChanged -= onDisableEffectDispatchingChanged;
            Configs.ChatVoting.WinnerSelectionMode.SettingChanged -= onVoteWinnerSelectionModeChanged;

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

            if (SaveManager.UseSaveData)
            {
                SaveManager.CollectSaveData -= SaveManager_CollectSaveData;
                SaveManager.LoadSaveData -= SaveManager_LoadSaveData;
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

            _offsetVoteNumbers = _voteStartCount++ % 2 != 0;

            int numOptions = numVoteOptions;
            _effectVoteSelection.NumOptions = numOptions;

            WeightedSelection<ChaosEffectInfo> effectSelection = ChaosEffectCatalog.GetAllActivatableEffects(new EffectCanActivateContext(_voteTimer.GetTimeRemaining()));

            EffectVoteInfo[] voteOptions = new EffectVoteInfo[numOptions];
            for (int i = 0; i < numOptions; i++)
            {
                int voteNumber = getVoteNumber(i);

                if (Configs.ChatVoting.IncludeRandomEffectInVote.Value && i == numOptions - 1)
                {
                    voteOptions[i] = EffectVoteInfo.Random(voteNumber);
                }
                else
                {
                    if (effectSelection.Count <= 0)
                    {
                        Log.Warning($"No activatable effects remain for vote {voteNumber}");
                        voteOptions[i] = new EffectVoteInfo(Nothing.EffectInfo, voteNumber);
                    }
                    else
                    {
                        voteOptions[i] = new EffectVoteInfo(effectSelection.GetAndRemoveRandom(_rng), voteNumber);
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
                    OnEffectVotingFinishedServer?.Invoke(new EffectVoteResult(_effectVoteSelection, voteResult));
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
            ChaosEffectDispatchArgs dispatchArgs;
            if (voteResult.IsRandom)
            {
                HashSet<ChaosEffectInfo> voteOptionEffects = new HashSet<ChaosEffectInfo>(_effectVoteSelection.GetVoteOptions()
                                                                                                              .Select(vote => vote.EffectInfo)
                                                                                                              .Where(effect => effect != null));

                effectInfo = ChaosEffectCatalog.PickActivatableEffect(_rng, EffectCanActivateContext.Now, voteOptionEffects);

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

        public bool CurrentVoteContains(ChaosEffectInfo effectInfo)
        {
            return _effectVoteSelection != null && _effectVoteSelection.IsVoteActive && _effectVoteSelection.GetVoteOptions().Any(voteInfo => voteInfo.EffectInfo == effectInfo);
        }

        public override float GetTimeUntilNextEffect()
        {
            return Mathf.Max(0f, _voteTimer.GetTimeRemaining());
        }
    }
}
