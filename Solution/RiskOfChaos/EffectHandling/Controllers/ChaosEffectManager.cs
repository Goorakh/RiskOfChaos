using RiskOfChaos.Components;
using RiskOfChaos.EffectHandling.Controllers.ChatVoting.Twitch;
using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling.Controllers
{
    [DisallowMultipleComponent]
    public class ChaosEffectManager : MonoBehaviour
    {
        static GameObject _chaosEffectManagerPrefab;

        static GameObject[] _effectActivationSignalerPrefabs = [];

        internal static void Load()
        {
            _chaosEffectManagerPrefab = NetPrefabs.CreateEmptyPrefabObject("ChaosEffectManager", true, [
                typeof(SetDontDestroyOnLoad),
                typeof(DestroyOnRunEnd),
                typeof(ChaosEffectManager),
                typeof(ChaosEffectDispatcher),
                typeof(ChaosEffectTracker),
                typeof(ChaosAlwaysActiveEffectsHandler),
                typeof(ChaosEffectActivationSoundHandler),
                typeof(ChaosEffectNameFormattersNetworker),
                typeof(ChaosNextEffectProvider)
            ]);

            static GameObject createBasicEffectActivationSignaler<TSignalerComponent>(Configs.ChatVoting.ChatVotingMode? requiredVotingMode) where TSignalerComponent : ChaosEffectActivationSignaler
            {
                Type signalerComponentType = typeof(TSignalerComponent);

                List<Type> prefabComponentTypes = [
                    signalerComponentType
                ];

                bool hasAnyEnableRequirement = requiredVotingMode.HasValue;
                if (hasAnyEnableRequirement)
                {
                    prefabComponentTypes.Add(typeof(ChaosEffectActivationSignalerEnableRequirements));
                }

                GameObject signalerPrefab = NetPrefabs.CreateEmptyPrefabObject(signalerComponentType.Name, false, prefabComponentTypes.ToArray());

                if (signalerPrefab.TryGetComponent(out ChaosEffectActivationSignalerEnableRequirements enableRequirements))
                {
                    enableRequirements.RequiredVotingMode = requiredVotingMode;

                    foreach (ChaosEffectActivationSignaler signalerComponent in signalerPrefab.GetComponents<ChaosEffectActivationSignaler>())
                    {
                        signalerComponent.enabled = false;
                    }
                }

                return signalerPrefab;
            }

            GameObject effectSignalerTimer = createBasicEffectActivationSignaler<ChaosEffectActivationSignaler_Timer>(Configs.ChatVoting.ChatVotingMode.Disabled);

            GameObject effectSignalerTwitchChatVote = createBasicEffectActivationSignaler<ChaosEffectActivationSignaler_TwitchVote>(Configs.ChatVoting.ChatVotingMode.Twitch);

            _effectActivationSignalerPrefabs = [
                effectSignalerTimer,
                effectSignalerTwitchChatVote
            ];

            Run.onRunStartGlobal += onRunStartGlobal;
        }

        static void onRunStartGlobal(Run run)
        {
            if (!NetworkServer.active)
                return;

            NetworkServer.Spawn(Instantiate(_chaosEffectManagerPrefab));
        }

        void Awake()
        {
            if (!NetworkServer.active)
            {
                enabled = false;
                return;
            }

            foreach (GameObject signalerPrefab in _effectActivationSignalerPrefabs)
            {
                GameObject signalerObject = Instantiate(signalerPrefab, transform);
            }
        }
    }
}
