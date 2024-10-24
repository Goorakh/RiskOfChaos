using RiskOfChaos.ConfigHandling;
using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.SaveHandling;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling.Controllers
{
    [DisallowMultipleComponent]
    [RequiredComponents(typeof(ChaosEffectDispatcher))]
    public class ChaosAlwaysActiveEffectsHandler : MonoBehaviour
    {
        readonly Dictionary<ChaosEffectIndex, List<ChaosEffectComponent>> _alwaysActiveEffects = [];

        bool _activeEffectsDirty;

        void Awake()
        {
            if (!NetworkServer.active)
            {
                enabled = false;
                return;
            }

            _alwaysActiveEffects.EnsureCapacity(ChaosEffectCatalog.AllTimedEffects.Length);
        }

        void OnEnable()
        {
            if (!NetworkServer.active)
                return;

            foreach (TimedEffectInfo timedEffectInfo in ChaosEffectCatalog.AllTimedEffects)
            {
                if (timedEffectInfo.AlwaysActiveEnabledConfig != null)
                {
                    timedEffectInfo.AlwaysActiveEnabledConfig.SettingChanged += onEffectAlwaysActiveConfigChanged;
                }

                if (timedEffectInfo.AlwaysActiveStackCountConfig != null)
                {
                    timedEffectInfo.AlwaysActiveStackCountConfig.SettingChanged += onEffectAlwaysActiveStackCountConfigChanged;
                }
            }

            markActiveEffectsDirty();
        }

        void OnDisable()
        {
            foreach (TimedEffectInfo timedEffectInfo in ChaosEffectCatalog.AllTimedEffects)
            {
                if (timedEffectInfo.AlwaysActiveEnabledConfig != null)
                {
                    timedEffectInfo.AlwaysActiveEnabledConfig.SettingChanged -= onEffectAlwaysActiveConfigChanged;
                }

                if (timedEffectInfo.AlwaysActiveStackCountConfig != null)
                {
                    timedEffectInfo.AlwaysActiveStackCountConfig.SettingChanged -= onEffectAlwaysActiveStackCountConfigChanged;
                }
            }

            foreach (List<ChaosEffectComponent> activeEffectInstances in _alwaysActiveEffects.Values)
            {
                foreach (ChaosEffectComponent effectComponent in activeEffectInstances)
                {
                    if (effectComponent)
                    {
                        Destroy(effectComponent.gameObject);
                    }
                }

                activeEffectInstances.Clear();
            }

            _alwaysActiveEffects.Clear();
            _activeEffectsDirty = false;
        }

        void FixedUpdate()
        {
            if (_activeEffectsDirty)
            {
                refreshEffects();
            }
        }

        void onEffectAlwaysActiveConfigChanged(object sender, ConfigChangedArgs<bool> e)
        {
            markActiveEffectsDirty();
        }

        void onEffectAlwaysActiveStackCountConfigChanged(object sender, ConfigChangedArgs<int> e)
        {
            markActiveEffectsDirty();
        }

        void markActiveEffectsDirty()
        {
            _activeEffectsDirty = true;
        }

        void refreshEffects()
        {
            _activeEffectsDirty = false;

            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return;
            }

            foreach (TimedEffectInfo timedEffectInfo in ChaosEffectCatalog.AllTimedEffects)
            {
                int targetAlwaysActiveCount = timedEffectInfo.AlwaysActiveCount;

                if (_alwaysActiveEffects.TryGetValue(timedEffectInfo.EffectIndex, out List<ChaosEffectComponent> activeEffectInstances))
                {
                    if (activeEffectInstances.Count == targetAlwaysActiveCount)
                        continue;

                    activeEffectInstances.EnsureCapacity(targetAlwaysActiveCount);
                }
                else
                {
                    if (targetAlwaysActiveCount == 0)
                        continue;

                    activeEffectInstances = new List<ChaosEffectComponent>(targetAlwaysActiveCount);
                    _alwaysActiveEffects.Add(timedEffectInfo.EffectIndex, activeEffectInstances);
                }

                if (targetAlwaysActiveCount > activeEffectInstances.Count)
                {
                    for (int i = activeEffectInstances.Count; i < targetAlwaysActiveCount; i++)
                    {
                        Xoroshiro128Plus effectRNG = new Xoroshiro128Plus((ulong)HashCode.Combine(Run.instance.seed, timedEffectInfo.Identifier, i));

                        ChaosEffectDispatchArgs dispatchArgs = new ChaosEffectDispatchArgs
                        {
                            DispatchFlags = EffectDispatchFlags.DontPlaySound | EffectDispatchFlags.DontSendChatMessage,
                            RNGSeed = effectRNG.nextUlong,
                            OverrideDurationType = TimedEffectType.AlwaysActive,
                            OverrideDuration = 1f
                        };

                        ChaosEffectComponent effectComponent = ChaosEffectDispatcher.Instance.DispatchEffectServer(timedEffectInfo, dispatchArgs);

                        if (effectComponent)
                        {
                            if (effectComponent.TryGetComponent(out ObjectSerializationComponent serializationComponent))
                            {
                                serializationComponent.enabled = false;
                            }
                        }

                        activeEffectInstances.Add(effectComponent);
                    }
                }
                else if (targetAlwaysActiveCount < activeEffectInstances.Count)
                {
                    for (int i = targetAlwaysActiveCount; i < activeEffectInstances.Count; i++)
                    {
                        ChaosEffectComponent effectComponent = activeEffectInstances[i];
                        if (effectComponent)
                        {
                            Destroy(effectComponent.gameObject);
                        }
                    }

                    activeEffectInstances.RemoveRange(targetAlwaysActiveCount, activeEffectInstances.Count - targetAlwaysActiveCount);
                }
            }
        }
    }
}
