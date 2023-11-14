using R2API.Networking;
using R2API.Networking.Interfaces;
using RiskOfChaos.EffectDefinitions;
using RiskOfChaos.Networking;
using RiskOfChaos.SaveHandling;
using RiskOfChaos.SaveHandling.DataContainers;
using RiskOfChaos.SaveHandling.DataContainers.EffectHandlerControllers;
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

        public delegate void TimedEffectDirtyDelegate(TimedEffectInfo effectInfo, TimedEffect effectInstance);
        public static event TimedEffectDirtyDelegate OnTimedEffectDirtyServer;

        readonly record struct ActiveTimedEffectInfo(TimedEffectInfo EffectInfo, TimedEffect EffectInstance, TimedEffectType TimedType, ChaosEffectDispatchArgs DispatchArgs)
        {
            public ActiveTimedEffectInfo(TimedEffectInfo effectInfo, TimedEffect effectInstance, ChaosEffectDispatchArgs dispatchArgs) : this(effectInfo, effectInstance, effectInstance.TimedType, dispatchArgs)
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

            public readonly byte[] GetSerializedData()
            {
                NetworkWriter writer = new NetworkWriter();
                try
                {
                    EffectInstance.Serialize(writer);
                }
                catch (Exception e)
                {
                    Log.Error_NoCallerPrefix($"Caught exception in {EffectInfo} {nameof(BaseEffect.Serialize)}: {e}");
                    return Array.Empty<byte>();
                }

                return writer.ToArray();
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
            NetworkedEffectSetSerializedDataMessage.OnReceive += NetworkedEffectSetSerializedDataMessage_OnReceive;

            _effectDispatcher.OnEffectAboutToDispatchServer += onEffectAboutToDispatchServer;
            _effectDispatcher.OnEffectDispatched += onEffectDispatched;

            Stage.onServerStageComplete += onServerStageComplete;

            if (NetworkServer.active && SaveManager.UseSaveData)
            {
                SaveManager.CollectSaveData += SaveManager_CollectSaveData;
                SaveManager.LoadSaveData += SaveManager_LoadSaveData;
            }
        }

        void Update()
        {
            if (NetworkServer.active)
            {
                for (int i = _activeTimedEffects.Count - 1; i >= 0; i--)
                {
                    ActiveTimedEffectInfo timedEffectInfo = _activeTimedEffects[i];
                    if (timedEffectInfo.MatchesFlag(TimedEffectFlags.FixedDuration))
                    {
                        if (timedEffectInfo.EffectInstance.TimeRemaining <= 0f)
                        {
#if DEBUG
                            Log.Debug($"Ending fixed duration timed effect {timedEffectInfo.EffectInfo} (ID={timedEffectInfo.EffectInstance.DispatchID})");
#endif

                            endTimedEffectAtIndex(i, true);
                            continue;
                        }
                    }

                    if (timedEffectInfo.EffectInstance.IsNetDirty)
                    {
#if DEBUG
                        Log.Debug($"Effect {timedEffectInfo.EffectInfo} (ID={timedEffectInfo.EffectInstance.DispatchID}) dirty, updating clients");
#endif

                        if (timedEffectInfo.EffectInfo.IsNetworked)
                        {
                            new NetworkedEffectSetSerializedDataMessage(timedEffectInfo.EffectInstance.DispatchID, timedEffectInfo.GetSerializedData()).Send(NetworkDestination.Clients);
                        }

                        OnTimedEffectDirtyServer?.Invoke(timedEffectInfo.EffectInfo, timedEffectInfo.EffectInstance);

                        timedEffectInfo.EffectInstance.IsNetDirty = false;
                    }
                }
            }
        }

        void OnDisable()
        {
            SingletonHelper.Unassign(ref _instance, this);

            NetworkedTimedEffectEndMessage.OnReceive -= NetworkedTimedEffectEndMessage_OnReceive;
            NetworkedEffectSetSerializedDataMessage.OnReceive -= NetworkedEffectSetSerializedDataMessage_OnReceive;

            _effectDispatcher.OnEffectAboutToDispatchServer -= onEffectAboutToDispatchServer;
            _effectDispatcher.OnEffectDispatched -= onEffectDispatched;

            Stage.onServerStageComplete -= onServerStageComplete;

            endTimedEffects(TimedEffectFlags.All, false);
            _activeTimedEffects.Clear();

            SaveManager.CollectSaveData -= SaveManager_CollectSaveData;
            SaveManager.LoadSaveData -= SaveManager_LoadSaveData;
        }

        void SaveManager_LoadSaveData(in SaveContainer container)
        {
            TimedEffectHandlerData data = container.TimedEffectHandlerData;
            if (data is null)
                return;

            if (data.ActiveTimedEffects is not null)
            {
                foreach (SerializableActiveEffect activeEffect in data.ActiveTimedEffects)
                {
                    ChaosEffectDispatchArgs dispatchArgs = activeEffect.DispatchArgs;
                    dispatchArgs.DispatchFlags = EffectDispatchFlags.LoadedFromSave;

                    _effectDispatcher.DispatchEffectFromSerializedDataServer(activeEffect.Effect.EffectInfo, activeEffect.SerializedEffectData.Bytes, dispatchArgs);
                }
            }
        }

        void SaveManager_CollectSaveData(ref SaveContainer container)
        {
            container.TimedEffectHandlerData = new TimedEffectHandlerData
            {
                ActiveTimedEffects = _activeTimedEffects.Select(e => new SerializableActiveEffect
                {
                    Effect = new SerializableEffect(e.EffectInfo),
                    DispatchArgs = e.DispatchArgs,
                    SerializedEffectData = e.GetSerializedData()
                }).ToArray()
            };
        }

        void onEffectAboutToDispatchServer(ChaosEffectInfo effectInfo, in ChaosEffectDispatchArgs args, ref bool willStart)
        {
            if (willStart)
            {
                for (int i = _activeTimedEffects.Count - 1; i >= 0; i--)
                {
                    TimedEffectInfo activeEffectInfo = _activeTimedEffects[i].EffectInfo;
                    if (effectInfo.IncompatibleEffects.Contains(activeEffectInfo) || activeEffectInfo.IncompatibleEffects.Contains(effectInfo))
                    {
#if DEBUG
                        Log.Debug($"Ending timed effect {activeEffectInfo} (ID={_activeTimedEffects[i].EffectInstance.DispatchID}) due to: incompatible effect about to start ({effectInfo})");
#endif
                        endTimedEffectAtIndex(i, true);
                    }
                }

                if (effectInfo is TimedEffectInfo timedEffect && !timedEffect.AllowDuplicates && timedEffect.TimedType == TimedEffectType.FixedDuration)
                {
                    foreach (ActiveTimedEffectInfo activeEffects in getActiveTimedEffectsFor(timedEffect))
                    {
                        activeEffects.EffectInstance.TimeRemaining = timedEffect.DurationSeconds;
                        willStart = false;
                    }
                }
            }
        }

        void onEffectDispatched(ChaosEffectInfo effectInfo, in ChaosEffectDispatchArgs dispatchArgs, BaseEffect effectInstance)
        {
            if (effectInfo is TimedEffectInfo timedEffectInfo && effectInstance is TimedEffect timedEffectInstance)
            {
                registerTimedEffect(new ActiveTimedEffectInfo(timedEffectInfo, timedEffectInstance, dispatchArgs));
            }
        }

        public bool AnyInstanceOfEffectActive(TimedEffectInfo effectInfo, in EffectCanActivateContext context)
        {
            foreach (ActiveTimedEffectInfo activeTimedEffectInfo in getActiveTimedEffectsFor(effectInfo))
            {
                if (!activeTimedEffectInfo.MatchesFlag(TimedEffectFlags.FixedDuration) ||
                    activeTimedEffectInfo.EffectInstance.TimeRemaining > context.Delay)
                {
                    return true;
                }
            }

            return false;
        }

        bool tryGetActiveEffectIndexByDispatchID(ulong dispatchID, out int index)
        {
            for (index = 0; index < _activeTimedEffects.Count; index++)
            {
                if (_activeTimedEffects[index].EffectInstance.DispatchID == dispatchID)
                {
                    return true;
                }
            }

            return false;
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
            if (tryGetActiveEffectIndexByDispatchID(dispatchID, out int activeEffectIndex))
            {
                endTimedEffectAtIndex(activeEffectIndex, sendClientMessage);

#if DEBUG
                Log.Debug($"Timed effect {_activeTimedEffects[activeEffectIndex].EffectInfo} (ID={dispatchID}) ended");
#endif
            }
            else
            {
                Log.Warning($"No timed effect registered with ID {dispatchID}");
            }
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

        void NetworkedEffectSetSerializedDataMessage_OnReceive(ulong effectDispatchID, byte[] serializedEffectData)
        {
            if (NetworkServer.active)
                return;

            if (tryGetActiveEffectIndexByDispatchID(effectDispatchID, out int activeEffectIndex))
            {
                NetworkReader reader = new NetworkReader(serializedEffectData);
                _activeTimedEffects[activeEffectIndex].EffectInstance.Deserialize(reader);
            }
            else
            {
                Log.Warning($"No active effect with id {effectDispatchID}");
            }
        }

        void registerTimedEffect(in ActiveTimedEffectInfo activeEffectInfo)
        {
            _activeTimedEffects.Add(activeEffectInfo);

            if (NetworkServer.active)
            {
                OnTimedEffectStartServer?.Invoke(activeEffectInfo.EffectInfo, activeEffectInfo.EffectInstance);
            }
        }

        IEnumerable<ActiveTimedEffectInfo> getActiveTimedEffectsFor(TimedEffectInfo effectInfo)
        {
            return _activeTimedEffects.Where(e => e.EffectInfo == effectInfo);
        }

        public bool IsTimedEffectActive(TimedEffectInfo effectInfo)
        {
            return getActiveTimedEffectsFor(effectInfo).Any();
        }

        public int GetEffectActiveCount(TimedEffectInfo effectInfo)
        {
            return getActiveTimedEffectsFor(effectInfo).Count();
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

        public void InvokeEventOnAllInstancesOfEffect<TEffect>(Func<TEffect, Action> eventGetter) where TEffect : TimedEffect
        {
            if (eventGetter is null)
                throw new ArgumentNullException(nameof(eventGetter));

            foreach (TEffect effectInstance in GetActiveEffectInstancesOfType<TEffect>())
            {
                Action action = eventGetter(effectInstance);
                action?.Invoke();
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
