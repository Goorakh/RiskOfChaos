using RiskOfChaos.EffectDefinitions;
using RiskOfChaos.Utilities;
using RoR2;
using RoR2.Audio;
using System;
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

        static ChaosEffectActivationCounter[] _effectActivationCounts;

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

        float _nextEffectDispatchTime = float.PositiveInfinity;

        Xoroshiro128Plus _nextEffectRNG;

        void OnEnable()
        {
            SingletonHelper.Assign(ref _instance, this);

            _nextEffectDispatchTime = 0f;
            _nextEffectRNG = new Xoroshiro128Plus(Run.instance.runRNG.nextUlong);

            Run.onRunDestroyGlobal += Run_onRunDestroyGlobal;

            Stage.onServerStageComplete += Stage_onServerStageComplete;

            resetAllEffectActivationCounters();
        }

        void OnDisable()
        {
            SingletonHelper.Unassign(ref _instance, this);

            Run.onRunDestroyGlobal -= Run_onRunDestroyGlobal;

            Stage.onServerStageComplete -= Stage_onServerStageComplete;

            resetAllEffectActivationCounters();
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
                    }

                    effectDispatcher.enabled = true;
                }
                else
                {
                    dispatcherObj.AddComponent<ChaosEffectDispatcher>();
                }
            };
        }

        void Update()
        {
            Run run = Run.instance;
            Stage stage = Stage.instance;

            if (!run || run.isRunStopwatchPaused || !stage)
                return;

            const float STAGE_START_OFFSET = 2f;
            if (run.GetRunStopwatch() >= _nextEffectDispatchTime && stage.entryTime.timeSince > STAGE_START_OFFSET)
            {
                _nextEffectDispatchTime += Main.TimeBetweenEffects.Value;

                dispatchRandomEffect();
            }
        }

        [ConCommand(commandName = "roc_startrandom", flags = ConVarFlags.SenderMustBeServer, helpText = "Dispatches a random effect")]
        static void CCDispatchRandomEffect(ConCommandArgs args)
        {
            if (!Run.instance || !_instance)
                return;

            _instance.dispatchRandomEffect();
        }

        void dispatchRandomEffect()
        {
            WeightedSelection<ChaosEffectInfo> weightedSelection = ChaosEffectCatalog.GetAllActivatableEffects();
            if (weightedSelection.Count > 0)
            {
                ChaosEffectInfo effect = weightedSelection.Evaluate(_nextEffectRNG.nextNormalizedFloat);
                dispatchEffect(effect);
            }
            else
            {
                Log.Error("No activatable effect!");
            }
        }

        [ConCommand(commandName = "roc_start", flags = ConVarFlags.SenderMustBeServer, helpText = "Dispatches an effect")]
        static void CCDispatchEffect(ConCommandArgs args)
        {
            if (!Run.instance || !_instance)
                return;

            int index = ChaosEffectCatalog.FindEffectIndex(args[0], false);
            if (index >= 0)
            {
                _instance.dispatchEffect(ChaosEffectCatalog.GetEffectInfo((uint)index));
            }
        }

        void dispatchEffect(in ChaosEffectInfo effect)
        {
            Chat.SendBroadcastChat(new Chat.SimpleChatMessage { baseToken = effect.GetActivationMessage() });

            incrementEffectActivationCounter(effect.EffectIndex);

            BaseEffect effectInstance = effect.InstantiateEffect(new Xoroshiro128Plus(_nextEffectRNG.nextUlong));
            effectInstance?.OnStart();

            playEffectActivatedSoundOnAllPlayerBodies();
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
            uint soundEventID = AkSoundEngine.GetIDFromString("Play_env_hiddenLab_laptop_sequence_fail");
            foreach (CharacterBody playerBody in PlayerUtils.GetAllPlayerBodies(true))
            {
                EntitySoundManager.EmitSoundServer(soundEventID, playerBody.gameObject);
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
