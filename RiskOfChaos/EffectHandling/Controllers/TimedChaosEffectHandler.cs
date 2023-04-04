using R2API.Networking;
using R2API.Networking.Interfaces;
using RiskOfChaos.EffectDefinitions;
using RiskOfChaos.Networking;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling.Controllers
{
    [ChaosController(false)]
    public class TimedChaosEffectHandler : MonoBehaviour
    {
        readonly struct TimedEffectInfo
        {
            public readonly ChaosEffectInfo EffectInfo;
            public readonly TimedEffect EffectInstance;

            public TimedEffectInfo(ChaosEffectInfo effectInfo, TimedEffect effectInstance)
            {
                EffectInfo = effectInfo;
                EffectInstance = effectInstance;
            }

            public readonly void End(bool sendClientMessage = true)
            {
                try
                {
                    EffectInstance.OnEnd();
                }
                catch (Exception ex)
                {
                    Log.Error($"Caught exception in {EffectInfo} {nameof(TimedEffect.OnEnd)}: {ex}");
                }

                if (NetworkServer.active)
                {
                    if (EffectInfo.IsNetworked && sendClientMessage)
                    {
                        new NetworkedTimedEffectEndMessage(EffectInfo).Send(NetworkDestination.Clients);
                    }
                }
            }

            public override readonly string ToString()
            {
                return EffectInfo.ToString();
            }
        }

        readonly List<TimedEffectInfo> _activeTimedEffects = new List<TimedEffectInfo>();

        ChaosEffectDispatcher _effectDispatcher;

        void Awake()
        {
            _effectDispatcher = GetComponent<ChaosEffectDispatcher>();
        }

        void OnEnable()
        {
            NetworkedTimedEffectEndMessage.OnReceive += NetworkedTimedEffectEndMessage_OnReceive;

            _effectDispatcher.OnEffectDispatched += onEffectDispatched;
        }

        void OnDisable()
        {
            NetworkedTimedEffectEndMessage.OnReceive -= NetworkedTimedEffectEndMessage_OnReceive;

            _effectDispatcher.OnEffectDispatched -= onEffectDispatched;

            endAllTimedEffects(false);
        }

        void onEffectDispatched(in ChaosEffectInfo effectInfo, EffectDispatchFlags dispatchFlags, BaseEffect effectInstance)
        {
            if (!NetworkServer.active)
                return;

            if ((dispatchFlags & EffectDispatchFlags.DontStopTimedEffects) == 0)
            {
                endAllTimedEffects();
            }

            if (effectInstance is TimedEffect timedEffectInstance)
            {
                registerTimedEffect(new TimedEffectInfo(effectInfo, timedEffectInstance));
            }
        }

        void endAllTimedEffects(bool sendClientMessage = true)
        {
            foreach (TimedEffectInfo timedEffect in _activeTimedEffects)
            {
                timedEffect.End(sendClientMessage);
            }

            _activeTimedEffects.Clear();
        }

        void NetworkedTimedEffectEndMessage_OnReceive(in ChaosEffectInfo effectInfo)
        {
            if (NetworkServer.active)
                return;

            for (int i = 0; i < _activeTimedEffects.Count; i++)
            {
                TimedEffectInfo timedEffect = _activeTimedEffects[i];
                if (timedEffect.EffectInfo == effectInfo)
                {
                    timedEffect.End(false);
                    _activeTimedEffects.RemoveAt(i);

#if DEBUG
                    Log.Debug($"Timed effect {effectInfo} ended");
#endif

                    return;
                }
            }

            Log.Warning($"{effectInfo} is not registered as a timed effect");
        }

        void registerTimedEffect(TimedEffectInfo effectInfo)
        {
            _activeTimedEffects.Add(effectInfo);
        }
    }
}
