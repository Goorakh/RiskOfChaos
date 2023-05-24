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

        readonly struct ActiveTimedEffectInfo
        {
            public readonly ChaosEffectInfo EffectInfo;
            public readonly TimedEffect EffectInstance;

            public readonly TimedEffectType TimedType;

            public ActiveTimedEffectInfo(in ChaosEffectInfo effectInfo, TimedEffect effectInstance)
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
                        new NetworkedTimedEffectEndMessage(EffectInstance.DispatchID).Send(NetworkDestination.Clients);
                    }
                }
            }

            public override readonly string ToString()
            {
                return EffectInfo.ToString();
            }
        }

        readonly List<ActiveTimedEffectInfo> _activeTimedEffects = new List<ActiveTimedEffectInfo>();

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

            if (NetworkServer.active)
            {
                TimedEffectCatalog.TimedEffectCanActivateOverride += timedEffectCanActivateOverride;
            }
        }

        void Update()
        {
            if (NetworkServer.active)
            {
                for (int i = _activeTimedEffects.Count - 1; i >= 0; i--)
                {
                    ActiveTimedEffectInfo timedEffectInfo = _activeTimedEffects[i];
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

            TimedEffectCatalog.TimedEffectCanActivateOverride -= timedEffectCanActivateOverride;

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
                registerTimedEffect(new ActiveTimedEffectInfo(effectInfo, timedEffectInstance));
            }
        }

        void timedEffectCanActivateOverride(TimedEffectInfo effect, in EffectCanActivateContext context, ref bool canActivate)
        {
            if (canActivate && !effect.AllowDuplicates)
            {
                bool effectAlreadyActive = IsTimedEffectActive(ChaosEffectCatalog.GetEffectInfo(effect.EffectIndex));
                if (effectAlreadyActive)
                {
                    canActivate = false;
#if DEBUG
                    Log.Debug($"Duplicate effect {effect} cannot activate");
#endif
                }
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
                ActiveTimedEffectInfo timedEffect = _activeTimedEffects[i];
                if (timedEffect.MatchesFlag(flags))
                {
#if DEBUG
                    Log.Debug($"Ending timed effect {timedEffect.EffectInfo} (ID={timedEffect.EffectInstance.DispatchID})");
#endif

                    timedEffect.End(sendClientMessage);
                    _activeTimedEffects.RemoveAt(i);
                }
            }
        }

        void NetworkedTimedEffectEndMessage_OnReceive(ulong effectDispatchID)
        {
            if (NetworkServer.active)
                return;

            for (int i = 0; i < _activeTimedEffects.Count; i++)
            {
                ActiveTimedEffectInfo timedEffect = _activeTimedEffects[i];
                if (timedEffect.EffectInstance != null && timedEffect.EffectInstance.DispatchID == effectDispatchID)
                {
                    timedEffect.End(false);
                    _activeTimedEffects.RemoveAt(i);

#if DEBUG
                    Log.Debug($"Timed effect {timedEffect.EffectInfo} (ID={effectDispatchID}) ended");
#endif

                    return;
                }
            }

            Log.Warning($"No timed effect registered with ID {effectDispatchID}");
        }

        void registerTimedEffect(in ActiveTimedEffectInfo effectInfo)
        {
            _activeTimedEffects.Add(effectInfo);
        }
        
        public bool IsTimedEffectActive(in ChaosEffectInfo effectInfo)
        {
            foreach (ActiveTimedEffectInfo timedEffect in _activeTimedEffects)
            {
                if (timedEffect.EffectInfo == effectInfo)
                {
                    return true;
                }
            }

            return false;
        }

        public int GetEffectActiveCount(in ChaosEffectInfo effectInfo)
        {
            int count = 0;

            foreach (ActiveTimedEffectInfo timedEffect in _activeTimedEffects)
            {
                if (timedEffect.EffectInfo == effectInfo)
                {
                    count++;
                }
            }

            return count;
        }

        public IEnumerable<TEffect> GetActiveEffectInstancesOfType<TEffect>() where TEffect : TimedEffect
        {
            foreach (ActiveTimedEffectInfo timedEffect in _activeTimedEffects)
            {
                if (timedEffect.EffectInstance is TEffect instance)
                {
                    yield return instance;
                }
            }
        }
    }
}
