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
    public struct ChaosEffectActivationCounter
    {
        public static readonly ChaosEffectActivationCounter EmptyCounter = new ChaosEffectActivationCounter(-1);

        public readonly int EffectIndex;

        public int TotalActivations { get; private set; }

        public int StageActivations { get; private set; }

        public ChaosEffectActivationCounter(int effectIndex)
        {
            EffectIndex = effectIndex;

            TotalActivations = 0;
            StageActivations = 0;

            if (effectIndex != -1)
            {
                Stage.onServerStageComplete += Stage_onServerStageComplete;
                Run.onRunDestroyGlobal += Run_onRunDestroyGlobal;
            }
        }

        void Stage_onServerStageComplete(Stage stage)
        {
            TotalActivations += StageActivations;
            StageActivations = 0;
        }

        void Run_onRunDestroyGlobal(Run run)
        {
            TotalActivations = 0;
            StageActivations = 0;
        }
    }

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
            _nextEffectRNG = null;
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

        static void dispatchEffect(in ChaosEffectInfo effect)
        {
            BaseEffect effectInstance = effect.InstantiateEffect(new Xoroshiro128Plus(_nextEffectRNG.nextUlong));
            effectInstance.OnStart();

            Chat.SendBroadcastChat(new Chat.SimpleChatMessage { baseToken = effect.GetActivationMessage() });

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

        static ChaosEffectActivationCounter getEffectActivationCounter(int effectIndex)
        {
            return ArrayUtils.GetSafe(_effectActivationCounts, effectIndex, ChaosEffectActivationCounter.EmptyCounter);
        }

        public static int GetTotalRunEffectActivationCount(int effectIndex)
        {
            return getEffectActivationCounter(effectIndex).TotalActivations;
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
