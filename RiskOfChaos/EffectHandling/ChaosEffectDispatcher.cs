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
        static Run.FixedTimeStamp _nextEffectDispatchTime = Run.FixedTimeStamp.positiveInfinity;

        static Xoroshiro128Plus _nextEffectRNG;

        [SystemInitializer]
        static void Init()
        {
            RoR2Application.onFixedUpdate += RoR2Application_onFixedUpdate;

            Run.onRunStartGlobal += Run_onRunStartGlobal;
            Run.onRunDestroyGlobal += Run_onRunDestroyGlobal;
        }

        static void Run_onRunStartGlobal(Run run)
        {
            if (!NetworkServer.active)
                return;

            _nextEffectRNG = new Xoroshiro128Plus(run.runRNG.nextUlong);
            _nextEffectDispatchTime = new Run.FixedTimeStamp(run.fixedTime);
        }

        static void Run_onRunDestroyGlobal(Run run)
        {
            _nextEffectRNG = null;
        }

        static void RoR2Application_onFixedUpdate()
        {
            if (!NetworkServer.active || !Stage.instance)
                return;

            const float STAGE_START_OFFSET = 2f;
            if (_nextEffectDispatchTime.hasPassed && Stage.instance.entryTime.timeSince > STAGE_START_OFFSET)
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
    }
}
