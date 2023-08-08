using R2API.Networking;
using R2API.Networking.Interfaces;
using RiskOfChaos.EffectDefinitions;
using RiskOfChaos.Networking;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling.Controllers
{
    [ChaosController(false)]
    public class TimedChaosEffectHandler : MonoBehaviour
    {
        static TimedChaosEffectHandler _instance;
        public static TimedChaosEffectHandler Instance => _instance;

        public delegate void TimedEffectStartDelegate(TimedEffectInfo effectInfo, TimedEffect effectInstance);
        public static event TimedEffectStartDelegate OnTimedEffectStartServer;

        public delegate void TimedEffectEndDelegate(ulong dispatchID);
        public static event TimedEffectEndDelegate OnTimedEffectEndServer;

        readonly record struct ActiveTimedEffectInfo(ChaosEffectInfo EffectInfo, TimedEffect EffectInstance, TimedEffectType TimedType)
        {
            public ActiveTimedEffectInfo(ChaosEffectInfo effectInfo, TimedEffect effectInstance) : this(effectInfo, effectInstance, effectInstance.TimedType)
            {
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
                    Log.Error_NoCallerPrefix($"Caught exception in {EffectInfo} {nameof(TimedEffect.OnEnd)}: {ex}");
                }

                if (EffectInfo.IsNetworked && sendClientMessage)
                {
                    if (NetworkServer.active)
                    {
                        new NetworkedTimedEffectEndMessage(EffectInstance.DispatchID).Send(NetworkDestination.Clients);
                    }
                    else
                    {
                        Log.Warning("Attempting to send effect end message to clients as non-server");
                    }
                }
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
                ChaosEffectCatalog.OverrideEffectCanActivate += effectCanActivateOverride;
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
                        Log.Debug($"Ending fixed duration timed effect {timedEffectInfo.EffectInfo} (ID={timedEffectInfo.EffectInstance.DispatchID})");
#endif

                        endTimedEffectAtIndex(i, true);
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

            ChaosEffectCatalog.OverrideEffectCanActivate -= effectCanActivateOverride;
            TimedEffectCatalog.TimedEffectCanActivateOverride -= timedEffectCanActivateOverride;

            endTimedEffects(TimedEffectFlags.All, false);
            _activeTimedEffects.Clear();
        }

        void onEffectDispatched(in ChaosEffectInfo effectInfo, EffectDispatchFlags dispatchFlags, BaseEffect effectInstance)
        {
            if (effectInstance is TimedEffect timedEffectInstance)
            {
                registerTimedEffect(new ActiveTimedEffectInfo(effectInfo, timedEffectInstance));
            }
        }

        void effectCanActivateOverride(in ChaosEffectInfo effectInfo, in EffectCanActivateContext context, ref bool canActivate)
        {
            if (!canActivate)
                return;

            foreach (ChaosEffectInfo incompatibleEffect in effectInfo.IncompatibleEffects)
            {
                foreach (ActiveTimedEffectInfo activeTimedEffectInfo in getActiveTimedEffectsFor(incompatibleEffect))
                {
                    if (!activeTimedEffectInfo.MatchesFlag(TimedEffectFlags.FixedDuration) ||
                        activeTimedEffectInfo.EffectInstance.TimeRemaining > context.Delay)
                    {
#if DEBUG
                        Log.Debug($"Effect {effectInfo} cannot activate: incompatible effect {activeTimedEffectInfo.EffectInfo} is active");
#endif

                        canActivate = false;
                        return;
                    }
                }
            }
        }

        void timedEffectCanActivateOverride(TimedEffectInfo effect, in EffectCanActivateContext context, ref bool canActivate)
        {
            if (!canActivate || effect.AllowDuplicates)
                return;

            foreach (ActiveTimedEffectInfo activeTimedEffectInfo in getActiveTimedEffectsFor(ChaosEffectCatalog.GetEffectInfo(effect.EffectIndex)))
            {
                if (!activeTimedEffectInfo.MatchesFlag(TimedEffectFlags.FixedDuration) ||
                    activeTimedEffectInfo.EffectInstance.TimeRemaining > context.Delay)
                {
#if DEBUG
                    Log.Debug($"Duplicate effect {effect} cannot activate");
#endif

                    canActivate = false;
                    return;
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
                    Log.Debug($"Ending timed effect matching flags {flags}: {timedEffect.EffectInfo} (ID={timedEffect.EffectInstance.DispatchID})");
#endif

                    endTimedEffectAtIndex(i, sendClientMessage);
                }
            }
        }

        void endTimedEffectWithDispatchID(ulong dispatchID, bool sendClientMessage)
        {
            for (int i = 0; i < _activeTimedEffects.Count; i++)
            {
                ActiveTimedEffectInfo timedEffect = _activeTimedEffects[i];
                if (timedEffect.EffectInstance != null && timedEffect.EffectInstance.DispatchID == dispatchID)
                {
                    endTimedEffectAtIndex(i, sendClientMessage);

#if DEBUG
                    Log.Debug($"Timed effect {timedEffect.EffectInfo} (ID={dispatchID}) ended");
#endif

                    return;
                }
            }

            Log.Warning($"No timed effect registered with ID {dispatchID}");
        }

        void endTimedEffectAtIndex(int index, bool sendClientMessage)
        {
            ActiveTimedEffectInfo timedEffect = _activeTimedEffects[index];

            if (NetworkServer.active)
            {
                OnTimedEffectEndServer?.Invoke(timedEffect.EffectInstance.DispatchID);
            }

            timedEffect.End(sendClientMessage);
            _activeTimedEffects.RemoveAt(index);
        }

        void NetworkedTimedEffectEndMessage_OnReceive(ulong effectDispatchID)
        {
            if (NetworkServer.active)
                return;

            endTimedEffectWithDispatchID(effectDispatchID, false);
        }

        void registerTimedEffect(in ActiveTimedEffectInfo effectInfo)
        {
            _activeTimedEffects.Add(effectInfo);

            if (NetworkServer.active)
            {
                if (TimedEffectCatalog.TryFindTimedEffectInfo(effectInfo.EffectInfo, out TimedEffectInfo timedEffectInfo))
                {
                    OnTimedEffectStartServer?.Invoke(timedEffectInfo, effectInfo.EffectInstance);
                }
                else
                {
                    Log.Error($"Could not find timed effect info for {effectInfo.EffectInfo}");
                }
            }
        }

        IEnumerable<ActiveTimedEffectInfo> getActiveTimedEffectsFor(ChaosEffectInfo effectInfo)
        {
            foreach (ActiveTimedEffectInfo timedEffect in _activeTimedEffects)
            {
                if (timedEffect.EffectInfo == effectInfo)
                {
                    yield return timedEffect;
                }
            }
        }

        public bool IsTimedEffectActive(in ChaosEffectInfo effectInfo)
        {
            return getActiveTimedEffectsFor(effectInfo).Any();
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

        [ConCommand(commandName = "roc_end_all_effects", flags = ConVarFlags.SenderMustBeServer, helpText = "Ends all active timed effects")]
        static void CCEndAllTimedEffects(ConCommandArgs args)
        {
            if (!NetworkServer.active || !Run.instance || !_instance || !_instance.enabled)
                return;

            _instance.endTimedEffects(TimedEffectFlags.All);
        }
    }
}
