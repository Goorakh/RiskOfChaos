using RiskOfChaos.Config;
using RiskOfChaos.EffectDefinitions;
using RiskOfChaos.Utilities;
using RoR2;
using RoR2.Audio;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling
{
    public class ChaosEffectDispatcher : MonoBehaviour
    {
        static GameObject _dispatcherObject;
        static GameObject dispatcherObject
        {
            get
            {
                if (!_dispatcherObject)
                {
                    _dispatcherObject = new GameObject("ChaosEffectDispatcher");
                    DontDestroyOnLoad(_dispatcherObject);
                }

                return _dispatcherObject;
            }
        }

        static ChaosEffectDispatcher _instance;
        public static ChaosEffectDispatcher Instance => _instance;

        static ChaosEffectActivationCounter[] _effectActivationCounts;

        static readonly List<TimedEffect> _activeTimedEffects = new List<TimedEffect>();

        static readonly AkEventIdArg _effectActivationSoundEventID = AkSoundEngine.GetIDFromString("Play_env_hiddenLab_laptop_sequence_fail");

        [SystemInitializer(typeof(ChaosEffectCatalog))]
        static void InitEffectActivationCounter()
        {
            _effectActivationCounts = ChaosEffectCatalog.PerEffectArray<ChaosEffectActivationCounter>();
            for (int i = 0; i < ChaosEffectCatalog.EffectCount; i++)
            {
                _effectActivationCounts[i] = new ChaosEffectActivationCounter(i);
            }
        }

        static void resetAllEffectActivationCounters()
        {
            for (int i = 0; i < _effectActivationCounts.Length; i++)
            {
                ref ChaosEffectActivationCounter activationCounter = ref _effectActivationCounts[i];
                activationCounter.StageActivations = 0;
                activationCounter.RunActivations = 0;
            }

#if DEBUG
            Log.Debug("reset all effect activation counters");
#endif
        }

        static void resetStageEffectActivationCounters()
        {
            for (int i = 0; i < _effectActivationCounts.Length; i++)
            {
                ref ChaosEffectActivationCounter activationCounter = ref _effectActivationCounts[i];
                activationCounter.StageActivations = 0;
            }

#if DEBUG
            Log.Debug("reset effect stage activation counters");
#endif
        }

        bool _wasRunStopwatchPausedLastUpdate = false;

        EffectDispatchTimer _unpausedEffectDispatchTimer = new EffectDispatchTimer(EffectDispatchTimerType.Unpaused);
        EffectDispatchTimer _pausedEffectDispatchTimer = new EffectDispatchTimer(EffectDispatchTimerType.Paused);

        ref EffectDispatchTimer currentEffectDispatchTimer
        {
            get
            {
                Run run = Run.instance;
                if (!run)
                {
                    Log.Warning("No run instance, using unpaused timer");
                    return ref _unpausedEffectDispatchTimer;
                }

                if (run.isRunStopwatchPaused)
                {
                    return ref _pausedEffectDispatchTimer;
                }
                else
                {
                    return ref _unpausedEffectDispatchTimer;
                }
            }
        }

        Xoroshiro128Plus _nextEffectRNG;

        void OnEnable()
        {
            SingletonHelper.Assign(ref _instance, this);

            _unpausedEffectDispatchTimer.Reset();
            _pausedEffectDispatchTimer.Reset();

            _wasRunStopwatchPausedLastUpdate = false;

            _nextEffectRNG = new Xoroshiro128Plus(Run.instance.runRNG.nextUlong);

            Run.onRunDestroyGlobal += Run_onRunDestroyGlobal;

            Stage.onServerStageComplete += Stage_onServerStageComplete;

            Configs.General.OnTimeBetweenEffectsChanged += onTimeBetweenEffectsConfigChanged;

            resetAllEffectActivationCounters();
        }

        void OnDisable()
        {
            SingletonHelper.Unassign(ref _instance, this);

            Run.onRunDestroyGlobal -= Run_onRunDestroyGlobal;

            Stage.onServerStageComplete -= Stage_onServerStageComplete;

            Configs.General.OnTimeBetweenEffectsChanged -= onTimeBetweenEffectsConfigChanged;

            resetAllEffectActivationCounters();

            endAllTimedEffects();
        }

        void Run_onRunDestroyGlobal(Run run)
        {
            enabled = false;
        }

        static void Stage_onServerStageComplete(Stage stage)
        {
            resetStageEffectActivationCounters();
        }

        [SystemInitializer]
        static void InitEventListeners()
        {
            Run.onRunStartGlobal += static _ =>
            {
                if (!NetworkServer.active)
                    return;
                
                GameObject dispatcherObj = dispatcherObject;

                if (dispatcherObj.TryGetComponent<ChaosEffectDispatcher>(out ChaosEffectDispatcher effectDispatcher))
                {
                    if (effectDispatcher.enabled)
                    {
                        Log.Warning("Starting run, but effect dispatcher is already enabled!");
                        effectDispatcher.enabled = false;
                    }

                    effectDispatcher.enabled = true;
                }
                else
                {
                    dispatcherObj.AddComponent<ChaosEffectDispatcher>();
                }
            };
        }

        static bool canDispatchEffects
        {
            get
            {
                if (PauseManager.isPaused && NetworkServer.dontListen)
                    return false;

                if (SceneExitController.isRunning)
                    return false;

                Run run = Run.instance;
                if (!run || run.isGameOverServer)
                    return false;

                const float STAGE_START_OFFSET = 2f;
                Stage stage = Stage.instance;
                if (!stage || stage.entryTime.timeSince < STAGE_START_OFFSET)
                    return false;

                return true;
            }
        }

        void onTimeBetweenEffectsConfigChanged()
        {
            if (!NetworkServer.active)
                return;

            _pausedEffectDispatchTimer.OnTimeBetweenEffectsChanged();
            _unpausedEffectDispatchTimer.OnTimeBetweenEffectsChanged();
        }

        void Update()
        {
            if (!canDispatchEffects)
                return;

            ref EffectDispatchTimer dispatchTimer = ref currentEffectDispatchTimer;

            updateStopwatchPaused(ref dispatchTimer);

            if (dispatchTimer.ShouldActivate())
            {
                dispatchTimer.ScheduleNextDispatch();
                DispatchRandomEffect();
            }
        }

        void updateStopwatchPaused(ref EffectDispatchTimer dispatchTimer)
        {
            bool isStopwatchPaused = Run.instance.isRunStopwatchPaused;
            if (_wasRunStopwatchPausedLastUpdate != isStopwatchPaused)
            {
                _wasRunStopwatchPausedLastUpdate = isStopwatchPaused;

                if (isStopwatchPaused)
                {
                    // Skip all the effect dispatches that should have already happened, but didn't since this timer hasn't updated
                    while (dispatchTimer.ShouldActivate())
                    {
                        dispatchTimer.ScheduleNextDispatch();
                    }
                }
            }
        }

        [ConCommand(commandName = "roc_startrandom", flags = ConVarFlags.SenderMustBeServer, helpText = "Dispatches a random effect")]
        static void CCDispatchRandomEffect(ConCommandArgs args)
        {
            if (!NetworkServer.active || !Run.instance || !_instance || !_instance.enabled)
                return;

            _instance.DispatchRandomEffect(EffectDispatchFlags.DontStopTimedEffects);
        }

        public void DispatchRandomEffect(EffectDispatchFlags dispatchFlags = EffectDispatchFlags.None)
        {
            WeightedSelection<ChaosEffectInfo> weightedSelection = ChaosEffectCatalog.GetAllActivatableEffects();

            ChaosEffectInfo effect;
            if (weightedSelection.Count > 0)
            {
                float nextNormalizedFloat = _nextEffectRNG.nextNormalizedFloat;
                effect = weightedSelection.Evaluate(nextNormalizedFloat);

#if DEBUG
                float effectWeight = weightedSelection.GetChoice(weightedSelection.EvaluateToChoiceIndex(nextNormalizedFloat)).weight;
                Log.Debug($"effect {effect.Identifier} selected, weight={effectWeight} ({effectWeight / weightedSelection.totalWeight:P} chance)");
#endif
            }
            else
            {
                Log.Warning("No activatable effects, defaulting to Nothing");

                effect = Nothing.EffectInfo;
            }

            dispatchEffect(effect, dispatchFlags);
        }

        [ConCommand(commandName = "roc_start", flags = ConVarFlags.SenderMustBeServer, helpText = "Dispatches an effect")]
        static void CCDispatchEffect(ConCommandArgs args)
        {
            if (!NetworkServer.active || !Run.instance || !_instance || !_instance.enabled)
                return;

            int index = ChaosEffectCatalog.FindEffectIndex(args[0]);
            if (index >= 0)
            {
                _instance.dispatchEffect(ChaosEffectCatalog.GetEffectInfo((uint)index), EffectDispatchFlags.DontStopTimedEffects);
            }
        }

        void dispatchEffect(in ChaosEffectInfo effect, EffectDispatchFlags dispatchFlags = EffectDispatchFlags.None)
        {
            Chat.SendBroadcastChat(new Chat.SimpleChatMessage { baseToken = effect.GetActivationMessage() });

            incrementEffectActivationCounter(effect.EffectIndex);

            BaseEffect effectInstance = effect.InstantiateEffect(new Xoroshiro128Plus(_nextEffectRNG.nextUlong));
            if (effectInstance != null)
            {
                if ((dispatchFlags & EffectDispatchFlags.DontStopTimedEffects) == 0)
                {
                    endAllTimedEffects();
                }

                try
                {
                    effectInstance.OnStart();
                }
                catch (Exception ex)
                {
                    Log.Error($"Caught exception in {effect} {nameof(BaseEffect.OnStart)}: {ex}");
                }

                if (effectInstance is TimedEffect timedEffectInstance)
                {
                    registerTimedEffect(timedEffectInstance);
                }
            }

            if ((dispatchFlags & EffectDispatchFlags.DontPlaySound) == 0)
            {
                playEffectActivatedSoundOnAllPlayerBodies();
            }
        }

        static void endAllTimedEffects()
        {
            foreach (TimedEffect timedEffect in _activeTimedEffects)
            {
                try
                {
                    timedEffect.OnEnd();
                }
                catch (Exception ex)
                {
                    Log.Error($"Caught exception in {timedEffect} {nameof(TimedEffect.OnEnd)}: {ex}");
                }
            }

            _activeTimedEffects.Clear();
        }

        static void registerTimedEffect(TimedEffect instance)
        {
            _activeTimedEffects.Add(instance);
        }

        static void incrementEffectActivationCounter(int effectIndex)
        {
            try
            {
                ref ChaosEffectActivationCounter activationCounter = ref getEffectActivationCounterUncheckedRef(effectIndex);
                activationCounter.StageActivations++;
                activationCounter.RunActivations++;

#if DEBUG
                Log.Debug($"increased effect activation counter: {activationCounter}");
#endif
            }
            catch (IndexOutOfRangeException ex)
            {
                Log.Error($"{nameof(IndexOutOfRangeException)} in {nameof(getEffectActivationCounterUncheckedRef)}, invalid effect index? {nameof(effectIndex)}={effectIndex}: {ex}");
            }
        }

        static void playEffectActivatedSoundOnAllPlayerBodies()
        {
            foreach (CharacterBody playerBody in PlayerUtils.GetAllPlayerBodies(true))
            {
                EntitySoundManager.EmitSoundServer(_effectActivationSoundEventID, playerBody.gameObject);
            }
        }

        static ref ChaosEffectActivationCounter getEffectActivationCounterUncheckedRef(int effectIndex)
        {
            return ref _effectActivationCounts[effectIndex];
        }

        static ChaosEffectActivationCounter getEffectActivationCounter(int effectIndex)
        {
            if (effectIndex < 0 || effectIndex >= _effectActivationCounts.Length)
                return ChaosEffectActivationCounter.EmptyCounter;

            return getEffectActivationCounterUncheckedRef(effectIndex);
        }

        public static int GetTotalRunEffectActivationCount(int effectIndex)
        {
            return getEffectActivationCounter(effectIndex).RunActivations;
        }

        public static int GetTotalStageEffectActivationCount(int effectIndex)
        {
            return getEffectActivationCounter(effectIndex).StageActivations;
        }

        public static int GetEffectActivationCount(int effectIndex, EffectActivationCountMode mode)
        {
            return mode switch
            {
                EffectActivationCountMode.PerStage => GetTotalStageEffectActivationCount(effectIndex),
                EffectActivationCountMode.PerRun => GetTotalRunEffectActivationCount(effectIndex),
                _ => 0,
            };
        }
    }
}
