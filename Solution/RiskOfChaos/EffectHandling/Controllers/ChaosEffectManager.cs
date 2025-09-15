using RiskOfChaos.Components;
using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling.Controllers.ChatVoting.Twitch;
using RiskOfChaos.SaveHandling;
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
        static GameObject[] _effectActivationSignalerPrefabs = [];

        [ContentInitializer]
        static void LoadContent(ContentIntializerArgs args)
        {
            // ChaosEffectManager
            {
                GameObject prefab = Prefabs.CreateNetworkedPrefab(nameof(RoCContent.NetworkedPrefabs.ChaosEffectManager), [
                    typeof(SetDontDestroyOnLoad),
                    typeof(AutoCreateOnRunStart),
                    typeof(DestroyOnRunEnd),
                    typeof(ChaosEffectManager),
                    typeof(ChaosEffectDispatcher),
                    typeof(ChaosEffectTracker),
                    typeof(ChaosAlwaysActiveEffectsHandler),
                    typeof(ChaosEffectActivationSoundHandler),
                    typeof(ChaosEffectNameFormattersNetworker),
                    typeof(ChaosNextEffectProvider),
                    typeof(ObjectSerializationComponent)
                ]);

                if (prefab.TryGetComponent(out ObjectSerializationComponent serializationComponent))
                {
                    serializationComponent.IsSingleton = true;
                }

                args.ContentPack.networkedObjectPrefabs.Add([prefab]);
            }

            GameObject createBasicEffectActivationSignaler<TSignalerComponent>(Configs.ChatVoting.ChatVotingMode? requiredVotingMode) where TSignalerComponent : ChaosEffectActivationSignaler
            {
                Type signalerComponentType = typeof(TSignalerComponent);

                List<Type> prefabComponentTypes = [
                    signalerComponentType,
                    typeof(NetworkParent),
                    typeof(DestroyOnRunEnd),
                    typeof(ObjectSerializationComponent)
                ];

                bool hasAnyEnableRequirement = requiredVotingMode.HasValue;
                if (hasAnyEnableRequirement)
                {
                    prefabComponentTypes.Add(typeof(ChaosEffectActivationSignalerEnableRequirements));
                }

                GameObject signalerPrefab = Prefabs.CreateNetworkedPrefab(signalerComponentType.Name, [.. prefabComponentTypes]);

                if (signalerPrefab.TryGetComponent(out ChaosEffectActivationSignalerEnableRequirements enableRequirements))
                {
                    enableRequirements.RequiredVotingMode = requiredVotingMode;

                    foreach (ChaosEffectActivationSignaler signalerComponent in signalerPrefab.GetComponents<ChaosEffectActivationSignaler>())
                    {
                        signalerComponent.enabled = false;
                    }
                }

                if (signalerPrefab.TryGetComponent(out NetworkIdentity networkIdentity))
                {
                    networkIdentity.serverOnly = true;
                }

                if (signalerPrefab.TryGetComponent(out ObjectSerializationComponent serializationComponent))
                {
                    serializationComponent.IsSingleton = true;
                }

                args.ContentPack.networkedObjectPrefabs.Add([signalerPrefab]);

                return signalerPrefab;
            }

            GameObject effectSignalerTimer = createBasicEffectActivationSignaler<ChaosEffectActivationSignaler_Timer>(Configs.ChatVoting.ChatVotingMode.Disabled);

            GameObject effectSignalerTwitchChatVote = createBasicEffectActivationSignaler<ChaosEffectActivationSignaler_TwitchVote>(Configs.ChatVoting.ChatVotingMode.Twitch);

            _effectActivationSignalerPrefabs = [
                effectSignalerTimer,
                effectSignalerTwitchChatVote
            ];
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
                NetworkServer.Spawn(signalerObject);
            }
        }
    }
}
