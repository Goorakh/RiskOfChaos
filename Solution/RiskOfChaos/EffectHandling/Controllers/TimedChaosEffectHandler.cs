using HG;
using R2API.Networking;
using R2API.Networking.Interfaces;
using RiskOfChaos.EffectDefinitions;
using RiskOfChaos.Networking;
using RiskOfChaos.SaveHandling;
using RiskOfChaos.SaveHandling.DataContainers;
using RiskOfChaos.SaveHandling.DataContainers.EffectHandlerControllers;
using RoR2;
using System;
using System.Collections;
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

        public delegate void TimedEffectStatusDelegate(TimedEffect effectInstance);

        public static event TimedEffectStatusDelegate OnTimedEffectStartServer;
        public static event TimedEffectStatusDelegate OnTimedEffectEndServer;
        public static event TimedEffectStatusDelegate OnTimedEffectDirtyServer;

        readonly record struct ActiveTimedEffectInfo(TimedEffect EffectInstance, ChaosEffectDispatchArgs DispatchArgs)
        {
            public readonly void End(bool sendClientMessage = true)
            {
                try
                {
                    EffectInstance.OnEnd();
                }
                catch (Exception ex)
                {
                    Log.Error_NoCallerPrefix($"Caught exception in {EffectInstance.EffectInfo} {nameof(TimedEffect.OnEnd)}: {ex}");
                }

                if (EffectInstance.EffectInfo.IsNetworked && sendClientMessage)
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
                    Log.Error_NoCallerPrefix($"Caught exception in {EffectInstance.EffectInfo} {nameof(BaseEffect.Serialize)}: {e}");
                    return [];
                }

                return writer.ToArray();
            }
        }

        readonly List<ActiveTimedEffectInfo> _activeTimedEffects = [];

        byte[] _effectStackCounts;

        ChaosEffectDispatcher _effectDispatcher;

        void Awake()
        {
            _effectDispatcher = GetComponent<ChaosEffectDispatcher>();

            _effectStackCounts = ChaosEffectCatalog.PerEffectArray<byte>();
        }

        void OnEnable()
        {
            SingletonHelper.Assign(ref _instance, this);

            NetworkedTimedEffectEndMessage.OnReceive += NetworkedTimedEffectEndMessage_OnReceive;
            NetworkedEffectSetSerializedDataMessage.OnReceive += NetworkedEffectSetSerializedDataMessage_OnReceive;

            _effectDispatcher.OnEffectAboutToDispatchServer += onEffectAboutToDispatchServer;
            _effectDispatcher.OnEffectAboutToStart += onEffectAboutToStart;

            Stage.onServerStageComplete += onServerStageComplete;

            if (_effectStackCounts != null)
            {
                Array.Clear(_effectStackCounts, 0, _effectStackCounts.Length);
            }

            if (NetworkServer.active)
            {
                static IEnumerator waitThenStartAlwaysActiveEffects()
                {
                    // HACK: Arbitrary delay for clients to be ready for the effects
                    yield return new WaitForSeconds(0.5f);

                    if (!Run.instance || !ChaosEffectDispatcher.Instance)
                        yield break;

                    Xoroshiro128Plus alwaysActiveEffectRNG = new Xoroshiro128Plus(Run.instance.seed);

                    foreach (TimedEffectInfo timedEffectInfo in ChaosEffectCatalog.AllTimedEffects)
                    {
                        for (int i = timedEffectInfo.AlwaysActiveCount - 1; i >= 0; i--)
                        {
                            ChaosEffectDispatchArgs dispatchArgs = new ChaosEffectDispatchArgs
                            {
                                DispatchFlags = EffectDispatchFlags.DontPlaySound | EffectDispatchFlags.DontSendChatMessage,
                                OverrideRNGSeed = alwaysActiveEffectRNG.nextUlong,
                                OverrideDurationType = TimedEffectType.AlwaysActive
                            };

                            ChaosEffectDispatcher.Instance.DispatchEffect(timedEffectInfo, dispatchArgs);
                        }
                    }
                }

                StartCoroutine(waitThenStartAlwaysActiveEffects());

                if (SaveManager.UseSaveData)
                {
                    SaveManager.CollectSaveData += SaveManager_CollectSaveData;
                    SaveManager.LoadSaveData += SaveManager_LoadSaveData;
                }
            }
        }

        void Update()
        {
            if (NetworkServer.active)
            {
                for (int i = _activeTimedEffects.Count - 1; i >= 0; i--)
                {
                    ActiveTimedEffectInfo timedEffectInfo = _activeTimedEffects[i];
                    if (timedEffectInfo.EffectInstance.MatchesFlag(TimedEffectFlags.FixedDuration))
                    {
                        if (timedEffectInfo.EffectInstance.TimeRemaining <= 0f)
                        {
#if DEBUG
                            Log.Debug($"Ending fixed duration timed effect {timedEffectInfo.EffectInstance.EffectInfo} (ID={timedEffectInfo.EffectInstance.DispatchID})");
#endif

                            endTimedEffectAtIndex(i, true);
                            continue;
                        }
                    }

                    if (timedEffectInfo.EffectInstance.IsNetDirty)
                    {
#if DEBUG
                        Log.Debug($"Effect {timedEffectInfo.EffectInstance.EffectInfo} (ID={timedEffectInfo.EffectInstance.DispatchID}) dirty, updating clients");
#endif

                        if (timedEffectInfo.EffectInstance.EffectInfo.IsNetworked)
                        {
                            new NetworkedEffectSetSerializedDataMessage(timedEffectInfo.EffectInstance.DispatchID, timedEffectInfo.GetSerializedData()).Send(NetworkDestination.Clients);
                        }

                        OnTimedEffectDirtyServer?.Invoke(timedEffectInfo.EffectInstance);

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
            _effectDispatcher.OnEffectAboutToStart -= onEffectAboutToStart;

            Stage.onServerStageComplete -= onServerStageComplete;

            endTimedEffects(TimedEffectFlags.All, false);
            _activeTimedEffects.Clear();

            if (_effectStackCounts != null)
            {
                Array.Clear(_effectStackCounts, 0, _effectStackCounts.Length);
            }

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
                    Effect = new SerializableEffect(e.EffectInstance.EffectInfo),
                    DispatchArgs = e.DispatchArgs,
                    SerializedEffectData = e.GetSerializedData()
                }).ToArray()
            };
        }

        void onEffectAboutToDispatchServer(ChaosEffectInfo effectInfo, in ChaosEffectDispatchArgs args, ref bool willStart)
        {
            if (!willStart)
                return;

            for (int i = _activeTimedEffects.Count - 1; i >= 0; i--)
            {
                TimedEffectInfo activeEffectInfo = _activeTimedEffects[i].EffectInstance.EffectInfo;
                if (effectInfo.IncompatibleEffects.Contains(activeEffectInfo) || activeEffectInfo.IncompatibleEffects.Contains(effectInfo))
                {
#if DEBUG
                    Log.Debug($"Ending timed effect {activeEffectInfo} (ID={_activeTimedEffects[i].EffectInstance.DispatchID}) due to: incompatible effect about to start ({effectInfo})");
#endif
                    endTimedEffectAtIndex(i, true);
                }
            }

            if (effectInfo is TimedEffectInfo timedEffect && !timedEffect.AllowDuplicates && timedEffect.CanStack)
            {
                foreach (ActiveTimedEffectInfo activeEffects in getActiveTimedEffectsFor(timedEffect))
                {
                    activeEffects.EffectInstance.MaxStocks += timedEffect.MaxStocks;
                    willStart = false;
                }
            }
        }

        void onEffectAboutToStart(BaseEffect effectInstance, in ChaosEffectDispatchArgs dispatchArgs)
        {
            if (effectInstance is TimedEffect timedEffectInstance)
            {
                registerTimedEffect(new ActiveTimedEffectInfo(timedEffectInstance, dispatchArgs));
            }
        }

        public bool AnyInstanceOfEffectActive(TimedEffectInfo effectInfo, in EffectCanActivateContext context)
        {
            foreach (ActiveTimedEffectInfo activeTimedEffectInfo in getActiveTimedEffectsFor(effectInfo))
            {
                if (!activeTimedEffectInfo.EffectInstance.MatchesFlag(TimedEffectFlags.FixedDuration) ||
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

            deductEffectStocks(TimedEffectFlags.UntilStageEnd);
        }

        void endTimedEffects(TimedEffectFlags flags, bool sendClientMessage = true)
        {
            for (int i = _activeTimedEffects.Count - 1; i >= 0; i--)
            {
                ActiveTimedEffectInfo timedEffect = _activeTimedEffects[i];
                if (timedEffect.EffectInstance.MatchesFlag(flags))
                {
#if DEBUG
                    Log.Debug($"Ending timed effect matching flags {flags}: {timedEffect.EffectInstance.EffectInfo} (ID={timedEffect.EffectInstance.DispatchID})");
#endif

                    endTimedEffectAtIndex(i, sendClientMessage);
                }
            }
        }

        void deductEffectStocks(TimedEffectFlags flags, bool sendClientMessage = true)
        {
            for (int i = _activeTimedEffects.Count - 1; i >= 0; i--)
            {
                ActiveTimedEffectInfo timedEffect = _activeTimedEffects[i];
                if (timedEffect.EffectInstance.MatchesFlag(flags))
                {
                    deductEffectStockAtIndex(i, sendClientMessage);
                }
            }
        }

        void deductEffectStockAtIndex(int index, bool sendClientMessage)
        {
            ActiveTimedEffectInfo timedEffect = _activeTimedEffects[index];

            timedEffect.EffectInstance.SpentStocks++;
            if (timedEffect.EffectInstance.StocksRemaining <= 0f)
            {
#if DEBUG
                Log.Debug($"Ending timed effect from stocks depleted: {timedEffect.EffectInstance.EffectInfo} (ID={timedEffect.EffectInstance.DispatchID})");
#endif

                endTimedEffectAtIndex(index, sendClientMessage);
            }
            else
            {
#if DEBUG
                Log.Debug($"Deducted effect stocks: {timedEffect.EffectInstance.EffectInfo} (ID={timedEffect.EffectInstance.DispatchID}), {timedEffect.EffectInstance.StocksRemaining} remaining");
#endif
            }
        }

        void endTimedEffectWithDispatchID(ulong dispatchID, bool sendClientMessage)
        {
            if (tryGetActiveEffectIndexByDispatchID(dispatchID, out int activeEffectIndex))
            {
#if DEBUG
                ActiveTimedEffectInfo timedEffectInfo = _activeTimedEffects[activeEffectIndex];
#endif

                endTimedEffectAtIndex(activeEffectIndex, sendClientMessage);

#if DEBUG
                Log.Debug($"Timed effect {timedEffectInfo.EffectInstance.EffectInfo} (ID={dispatchID}) ended");
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
                OnTimedEffectEndServer?.Invoke(timedEffect.EffectInstance);
            }

            if (_effectStackCounts != null)
            {
                _effectStackCounts[(int)timedEffect.EffectInstance.EffectInfo.EffectIndex]--;
            }

            _activeTimedEffects.RemoveAt(index);
            timedEffect.End(sendClientMessage);
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
            if (_effectStackCounts != null)
            {
                _effectStackCounts[(int)activeEffectInfo.EffectInstance.EffectInfo.EffectIndex]++;
            }

            _activeTimedEffects.Add(activeEffectInfo);

            if (NetworkServer.active)
            {
                OnTimedEffectStartServer?.Invoke(activeEffectInfo.EffectInstance);
            }
        }

        IEnumerable<ActiveTimedEffectInfo> getActiveTimedEffectsFor(TimedEffectInfo effectInfo)
        {
            foreach (ActiveTimedEffectInfo timedEffect in _activeTimedEffects)
            {
                if (timedEffect.EffectInstance.EffectInfo == effectInfo)
                {
                    yield return timedEffect;
                }
            }
        }

        public void EndEffectServer(TimedEffect effect)
        {
            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return;
            }

            endTimedEffectWithDispatchID(effect.DispatchID, true);
        }

        public bool IsTimedEffectActive(TimedEffectInfo effectInfo)
        {
            return GetEffectStackCount(effectInfo) > 0;
        }

        public int GetEffectStackCount(TimedEffectInfo effectInfo)
        {
            return ArrayUtils.GetSafe(_effectStackCounts, (int)effectInfo.EffectIndex);
        }

        public IEnumerable<TimedEffect> GetActiveEffects(TimedEffectInfo effectInfo)
        {
            foreach (ActiveTimedEffectInfo activeEffect in _activeTimedEffects)
            {
                if (activeEffect.EffectInstance.EffectInfo == effectInfo)
                {
                    yield return activeEffect.EffectInstance;
                }
            }
        }

        public TimedEffect[] GetAllActiveEffects()
        {
            if (_activeTimedEffects.Count == 0)
            {
                return [];
            }

            TimedEffect[] result = new TimedEffect[_activeTimedEffects.Count];
            for (int i = _activeTimedEffects.Count - 1; i >= 0; i--)
            {
                result[i] = _activeTimedEffects[i].EffectInstance;
            }

            return result;
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

            _instance.endTimedEffects(TimedEffectFlags.UntilStageEnd | TimedEffectFlags.FixedDuration | TimedEffectFlags.Permanent);
        }

        [ConCommand(commandName = "roc_list_active_effects", helpText = "Prints all active effects to the console")]
        static void CCListActiveEffects(ConCommandArgs args)
        {
            if (!_instance || !_instance.enabled)
                return;

            foreach (ActiveTimedEffectInfo activeEffect in _instance._activeTimedEffects)
            {
                Debug.Log($"{activeEffect.EffectInstance.EffectInfo.GetLocalDisplayName(EffectNameFormatFlags.RuntimeFormatArgs)} (Identifier={activeEffect.EffectInstance.EffectInfo.Identifier}, ID={activeEffect.EffectInstance.DispatchID})");
            }
        }

        [ConCommand(commandName = "roc_end_effect", flags = ConVarFlags.SenderMustBeServer, helpText = "Ends an effect with the specified ID, use roc_list_active_effects to get the IDs of all active effects")]
        static void CCEndTimedEffect(ConCommandArgs args)
        {
            if (!NetworkServer.active || !Run.instance || !_instance || !_instance.enabled)
                return;

            args.CheckArgumentCount(1);

            ulong dispatchID = args.GetArgULong(0);
            _instance.endTimedEffectWithDispatchID(dispatchID, true);
        }
    }
}
