using HG;
using RiskOfChaos.EffectDefinitions;
using RoR2;
using RoR2.Audio;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling
{
    public static class ChaosEffectDispatcher
    {
        static float _nextEffectDispatchTime = float.PositiveInfinity;

        static Xoroshiro128Plus _nextEffectRNG;

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

        [SystemInitializer]
        static void InitEventListeners()
        {
            RoR2Application.onFixedUpdate += RoR2Application_onFixedUpdate;

            Run.onRunStartGlobal += Run_onRunStartGlobal;
            Run.onRunDestroyGlobal += Run_onRunDestroyGlobal;

            Stage.onServerStageComplete += Stage_onServerStageComplete;
        }

        static void Run_onRunStartGlobal(Run run)
        {
            if (NetworkServer.active)
            {
                _nextEffectRNG = new Xoroshiro128Plus(run.runRNG.nextUlong);
                _nextEffectDispatchTime = 0f;
            }
            else
            {
                _nextEffectDispatchTime = float.PositiveInfinity;
            }
        }

        static void Run_onRunDestroyGlobal(Run run)
        {
            const string LOG_PREFIX = $"{nameof(ChaosEffectDispatcher)}.{nameof(Run_onRunDestroyGlobal)} ";

            _nextEffectRNG = null;

            for (int i = 0; i < _effectActivationCounts.Length; i++)
            {
                ref ChaosEffectActivationCounter activationCounter = ref _effectActivationCounts[i];
                activationCounter.StageActivations = 0;
                activationCounter.RunActivations = 0;
            }

#if DEBUG
            Log.Debug(LOG_PREFIX + $"reset all effect activation counters");
#endif
        }

        static void Stage_onServerStageComplete(Stage stage)
        {
            const string LOG_PREFIX = $"{nameof(ChaosEffectDispatcher)}.{nameof(Stage_onServerStageComplete)} ";

            for (int i = 0; i < _effectActivationCounts.Length; i++)
            {
                ref ChaosEffectActivationCounter activationCounter = ref _effectActivationCounts[i];
                activationCounter.StageActivations = 0;
            }

#if DEBUG
            Log.Debug(LOG_PREFIX + $"reset effect stage activation counters");
#endif
        }

        static void RoR2Application_onFixedUpdate()
        {
            if (!NetworkServer.active || !Run.instance || !Stage.instance)
                return;

            const float STAGE_START_OFFSET = 2f;
            if (Run.instance.GetRunStopwatch() >= _nextEffectDispatchTime && Stage.instance.entryTime.timeSince > STAGE_START_OFFSET)
            {
                dispatchRandomEffect();

                _nextEffectDispatchTime += Main.TimeBetweenEffects.Value;
            }
        }

        [ConCommand(commandName = "roc_startrandom", flags = ConVarFlags.SenderMustBeServer, helpText = "Dispatches a random effect")]
        static void CCDispatchRandomEffect(ConCommandArgs args)
        {
            if (!Run.instance)
                return;

            dispatchRandomEffect();
        }

        static void dispatchRandomEffect()
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
            if (!Run.instance)
                return;

            int index = ChaosEffectCatalog.FindEffectIndex(args[0], false);
            if (index >= 0)
            {
                dispatchEffect(ChaosEffectCatalog.GetEffectInfo((uint)index));
            }
        }

        static void dispatchEffect(in ChaosEffectInfo effect)
        {
            const string LOG_PREFIX = $"{nameof(ChaosEffectDispatcher)}.{nameof(dispatchEffect)} ";

            Chat.SendBroadcastChat(new Chat.SimpleChatMessage { baseToken = effect.GetActivationMessage() });

            BaseEffect effectInstance = effect.InstantiateEffect(new Xoroshiro128Plus(_nextEffectRNG.nextUlong));
            effectInstance?.OnStart();

            try
            {
                ref ChaosEffectActivationCounter activationCounter = ref getEffectActivationCounterUncheckedRef(effect.EffectIndex);
                activationCounter.StageActivations++;
                activationCounter.RunActivations++;

#if DEBUG
                Log.Debug(LOG_PREFIX + $"increased effect activation counter: {activationCounter}");
#endif
            }
            catch (IndexOutOfRangeException ex)
            {
                Log.Error(LOG_PREFIX + $"{nameof(IndexOutOfRangeException)} in {nameof(getEffectActivationCounterUncheckedRef)}, invalid effect index? {nameof(effect.EffectIndex)}={effect.EffectIndex}: {ex}");
            }

            playEffectActivatedSoundOnAllPlayerBodies();
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
