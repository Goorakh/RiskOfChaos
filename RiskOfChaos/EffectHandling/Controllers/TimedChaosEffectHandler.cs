using R2API.Networking;
using R2API.Networking.Interfaces;
using RiskOfChaos.EffectDefinitions;
using RiskOfChaos.Networking;
using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling.Controllers
{
    [ChaosController(false)]
    public class TimedChaosEffectHandler : MonoBehaviour
    {
        static TimedChaosEffectHandler _instance;
        public static TimedChaosEffectHandler Instance => _instance;

        [Flags]
        enum TimedEffectFlags
        {
            None = 0,
            UntilNextEffect = 1 << TimedEffectType.UntilNextEffect,
            UntilStageEnd = 1 << TimedEffectType.UntilStageEnd,
            FixedDuration = 1 << TimedEffectType.FixedDuration,
            All = ~0b0
        }

        readonly struct TimedEffectInfo
        {
            public readonly ChaosEffectInfo EffectInfo;
            public readonly TimedEffect EffectInstance;

            public readonly TimedEffectType TimedType;

            public TimedEffectInfo(in ChaosEffectInfo effectInfo, TimedEffect effectInstance)
            {
                EffectInfo = effectInfo;
                EffectInstance = effectInstance;

                TimedType = effectInstance.TimedType;
            }

            public readonly bool MatchesFlag(TimedEffectFlags flags)
            {
                return (flags & (TimedEffectFlags)(1 << (byte)TimedType)) != 0;
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
            SingletonHelper.Assign(ref _instance, this);

            NetworkedTimedEffectEndMessage.OnReceive += NetworkedTimedEffectEndMessage_OnReceive;

            _effectDispatcher.OnEffectDispatched += onEffectDispatched;

            Stage.onServerStageComplete += onServerStageComplete;
        }

        void Update()
        {
            if (NetworkServer.active)
            {
                for (int i = _activeTimedEffects.Count - 1; i >= 0; i--)
                {
                    TimedEffectInfo timedEffectInfo = _activeTimedEffects[i];
                    if (!timedEffectInfo.MatchesFlag(TimedEffectFlags.FixedDuration))
                        continue;

                    if (timedEffectInfo.EffectInstance.TimeRemaining <= 0f)
                    {
#if DEBUG
                        Log.Debug($"Ending fixed duration timed effect {timedEffectInfo.EffectInfo} (i={i})");
#endif

                        timedEffectInfo.End();
                        _activeTimedEffects.RemoveAt(i);
                    }
                }
            }
        }

        void OnDisable()
        {
            SingletonHelper.Unassign(ref _instance, this);

            NetworkedTimedEffectEndMessage.OnReceive -= NetworkedTimedEffectEndMessage_OnReceive;

            _effectDispatcher.OnEffectDispatched -= onEffectDispatched;

            Stage.onServerStageComplete -= onServerStageComplete;

            endTimedEffects(TimedEffectFlags.All, false);
            _activeTimedEffects.Clear();
        }

        void onEffectDispatched(in ChaosEffectInfo effectInfo, EffectDispatchFlags dispatchFlags, BaseEffect effectInstance)
        {
            if (NetworkServer.active && (dispatchFlags & EffectDispatchFlags.DontStopTimedEffects) == 0)
            {
                endTimedEffects(TimedEffectFlags.UntilNextEffect);
            }

            if (effectInstance is TimedEffect timedEffectInstance)
            {
                registerTimedEffect(new TimedEffectInfo(effectInfo, timedEffectInstance));
            }
        }

        void onServerStageComplete(Stage stage)
        {
            if (!NetworkServer.active)
                return;

            endTimedEffects(TimedEffectFlags.UntilStageEnd);
        }

        void endTimedEffects(TimedEffectFlags flags, bool sendClientMessage = true)
        {
            for (int i = _activeTimedEffects.Count - 1; i >= 0; i--)
            {
                TimedEffectInfo timedEffect = _activeTimedEffects[i];
                if (timedEffect.MatchesFlag(flags))
                {
#if DEBUG
                    Log.Debug($"Ending timed effect {timedEffect.EffectInfo} (i={i})");
#endif

                    timedEffect.End(sendClientMessage);
                    _activeTimedEffects.RemoveAt(i);
                }
            }
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
                    Log.Debug($"Timed effect {effectInfo} (i={i}) ended");
#endif

                    return;
                }
            }

            Log.Warning($"{effectInfo} is not registered as a timed effect");
        }

        void registerTimedEffect(in TimedEffectInfo effectInfo)
        {
            _activeTimedEffects.Add(effectInfo);
        }

        public bool IsTimedEffectActive(in ChaosEffectInfo effectInfo)
        {
            foreach (TimedEffectInfo timedEffect in _activeTimedEffects)
            {
                if (timedEffect.EffectInfo == effectInfo)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
