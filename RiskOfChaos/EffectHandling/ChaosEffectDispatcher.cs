using RiskOfChaos.EffectDefinitions;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling
{
    public static class ChaosEffectDispatcher
    {
        static Run.FixedTimeStamp _nextEffectDispatchTime = Run.FixedTimeStamp.positiveInfinity;

        static Xoroshiro128Plus _nextEffectRNG;

        [SystemInitializer]
        static void Init()
        {
            RoR2Application.onFixedUpdate += RoR2Application_onFixedUpdate;

            Stage.onServerStageBegin += Stage_onServerStageBegin;

            Run.onRunStartGlobal += Run_onRunStartGlobal;
            Run.onRunDestroyGlobal += Run_onRunDestroyGlobal;
        }

        static void Stage_onServerStageBegin(Stage stage)
        {
            const float START_OFFSET = 2f;
            _nextEffectDispatchTime = stage.entryTime + START_OFFSET;
        }

        static void Run_onRunStartGlobal(Run run)
        {
            if (!NetworkServer.active)
                return;

            _nextEffectRNG = new Xoroshiro128Plus(run.runRNG.nextUlong);
        }

        static void Run_onRunDestroyGlobal(Run run)
        {
            _nextEffectRNG = null;
        }

        static void RoR2Application_onFixedUpdate()
        {
            if (!NetworkServer.active)
                return;

            if (_nextEffectDispatchTime.hasPassed)
            {
                dispatchRandomEffect();

                if (Main.EffectActivationMode.Value == ChaosEffectMode.OnTimer)
                {
                    _nextEffectDispatchTime = Run.FixedTimeStamp.now + Main.TimeBetweenEffects.Value;
                }
                else
                {
                    _nextEffectDispatchTime = Run.FixedTimeStamp.positiveInfinity;
                }
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
        }
    }
}
