using R2API.Networking;
using R2API.Networking.Interfaces;
using RiskOfChaos.EffectDefinitions;
using RiskOfChaos.Networking;
using RiskOfChaos.SaveHandling;
using RiskOfChaos.SaveHandling.DataContainers;
using RiskOfChaos.SaveHandling.DataContainers.EffectHandlerControllers;
using RoR2;
using System;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling.Controllers
{
    [ChaosController(false)]
    public class ChaosEffectDispatcher : MonoBehaviour
    {
        static ChaosEffectDispatcher _instance;
        public static ChaosEffectDispatcher Instance => _instance;

        public delegate void EffectDispatchedDelegate(ChaosEffectInfo effectInfo, EffectDispatchFlags dispatchFlags, BaseEffect effectInstance);
        public event EffectDispatchedDelegate OnEffectDispatched;

        public delegate void EffectAboutToDispatchDelegate(ChaosEffectInfo effectInfo, EffectDispatchFlags dispatchFlags, bool willStart);
        public event EffectAboutToDispatchDelegate OnEffectAboutToDispatchServer;

        ChaosEffectActivationSignaler[] _effectActivationSignalers;

        Xoroshiro128Plus _effectRNG;
        ulong _effectDispatchCount;

        void Awake()
        {
            _effectActivationSignalers = GetComponents<ChaosEffectActivationSignaler>();
        }

        void OnEnable()
        {
            SingletonHelper.Assign(ref _instance, this);

            if (NetworkServer.active)
            {
                foreach (ChaosEffectActivationSignaler activationSignaler in _effectActivationSignalers)
                {
                    activationSignaler.SignalShouldDispatchEffect += ActivationSignaler_SignalShouldDispatchEffect;
                }

                if (Run.instance)
                {
                    _effectRNG = new Xoroshiro128Plus(Run.instance.seed);
                }

                _effectDispatchCount = 0;

                if (SaveManager.UseSaveData)
                {
                    SaveManager.CollectSaveData += SaveManager_CollectSaveData;
                    SaveManager.LoadSaveData += SaveManager_LoadSaveData;
                }
            }
            else
            {
                NetworkedEffectDispatchedMessage.OnReceive += NetworkedEffectDispatchedMessage_OnReceive;
            }
        }

        void Update()
        {
            if (!NetworkServer.active)
                return;

            foreach (ChaosEffectInfo effectInfo in ChaosEffectCatalog.AllEffects)
            {
                if (effectInfo.IsActivationShortcutPressed)
                {
                    if (effectInfo.CanActivate(EffectCanActivateContext.Now))
                    {
                        dispatchEffect(effectInfo);
                    }
                    else
                    {
                        ChaosEffectActivationSoundHandler.PlayEffectActivatedSound();

                        Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                        {
                            baseToken = "CHAOS_EFFECT_SHORTCUT_CANNOT_ACTIVATE",
                            paramTokens = new string[] { effectInfo.GetDisplayName(EffectNameFormatFlags.RuntimeFormatArgs) }
                        });
                    }
                }
            }
        }

        void OnDisable()
        {
            SingletonHelper.Unassign(ref _instance, this);

            foreach (ChaosEffectActivationSignaler activationSignaler in _effectActivationSignalers)
            {
                activationSignaler.SignalShouldDispatchEffect -= ActivationSignaler_SignalShouldDispatchEffect;
            }

            _effectRNG = null;

            NetworkedEffectDispatchedMessage.OnReceive -= NetworkedEffectDispatchedMessage_OnReceive;

            SaveManager.CollectSaveData -= SaveManager_CollectSaveData;
            SaveManager.LoadSaveData -= SaveManager_LoadSaveData;
        }

        void SaveManager_CollectSaveData(ref SaveContainer container)
        {
            container.DispatcherData = new EffectDispatcherData
            {
                EffectRNG = new SerializableRng(_effectRNG),
                EffectDispatchCount = _effectDispatchCount
            };
        }

        void SaveManager_LoadSaveData(in SaveContainer container)
        {
            EffectDispatcherData data = container.DispatcherData;
            if (data is null)
                return;

            _effectRNG = data.EffectRNG;
            _effectDispatchCount = data.EffectDispatchCount;
        }

        public void SkipAllScheduledEffects()
        {
            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return;
            }

            foreach (ChaosEffectActivationSignaler activationSignaler in _effectActivationSignalers)
            {
                if (activationSignaler && activationSignaler.enabled)
                {
                    activationSignaler.SkipAllScheduledEffects();
                }
            }
        }

        public void RewindEffectScheduling(float numSeconds)
        {
            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return;
            }

            foreach (ChaosEffectActivationSignaler activationSignaler in _effectActivationSignalers)
            {
                if (activationSignaler && activationSignaler.enabled)
                {
                    activationSignaler.RewindEffectScheduling(numSeconds);
                }
            }
        }

        public void DispatchEffectFromSerializedDataServer(ChaosEffectInfo effectInfo, byte[] serializedEffectData, EffectDispatchFlags flags = EffectDispatchFlags.None)
        {
            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return;
            }

            dispatchEffectFromSerializedData(effectInfo, serializedEffectData, flags);
        }

        BaseEffect dispatchEffectFromSerializedData(ChaosEffectInfo effectInfo, byte[] serializedEffectData, EffectDispatchFlags flags = EffectDispatchFlags.None)
        {
            BaseEffect effectInstance = dispatchEffect(effectInfo, flags | EffectDispatchFlags.DontStart);
            if (effectInstance != null)
            {
                NetworkReader networkReader = new NetworkReader(serializedEffectData);

                try
                {
                    effectInstance.Deserialize(networkReader);
                }
                catch (Exception ex)
                {
                    Log.Error_NoCallerPrefix($"Caught exception in {effectInfo} {nameof(BaseEffect.Deserialize)}: {ex}");
                }

                if ((flags & EffectDispatchFlags.DontStart) == 0)
                {
                    startEffect(effectInfo, flags, effectInstance);
                }
            }

            return effectInstance;
        }

        void NetworkedEffectDispatchedMessage_OnReceive(ChaosEffectInfo effectInfo, EffectDispatchFlags dispatchFlags, byte[] serializedEffectData)
        {
            if (NetworkServer.active)
                return;

            BaseEffect effectInstance = dispatchEffectFromSerializedData(effectInfo, serializedEffectData, dispatchFlags | EffectDispatchFlags.DontStart);
            if (effectInstance != null)
            {
                startEffect(effectInfo, dispatchFlags, effectInstance);

#if DEBUG
                Log.Debug($"Started networked effect {effectInfo}");
#endif
            }
        }

        [ConCommand(commandName = "roc_startrandom", flags = ConVarFlags.SenderMustBeServer, helpText = "Dispatches a random effect")]
        static void CCDispatchRandomEffect(ConCommandArgs args)
        {
            if (!NetworkServer.active || !Run.instance || !_instance || !_instance.enabled)
                return;

            _instance.dispatchEffect(ChaosEffectCatalog.PickActivatableEffect(RoR2Application.rng, EffectCanActivateContext.Now));
        }

        [ConCommand(commandName = "roc_start", flags = ConVarFlags.SenderMustBeServer, helpText = "Dispatches an effect")]
        static void CCDispatchEffect(ConCommandArgs args)
        {
            if (!NetworkServer.active || !Run.instance || !_instance || !_instance.enabled)
                return;

            ChaosEffectIndex index = ChaosEffectCatalog.FindEffectIndex(args[0]);
            if (index > ChaosEffectIndex.Invalid)
            {
                _instance.dispatchEffect(ChaosEffectCatalog.GetEffectInfo(index));
            }
        }

        void ActivationSignaler_SignalShouldDispatchEffect(ChaosEffectInfo effect, EffectDispatchFlags dispatchFlags = EffectDispatchFlags.None)
        {
            if (Configs.General.DisableEffectDispatching.Value)
                return;

            DispatchEffect(effect, dispatchFlags);
        }

        public void DispatchEffect(ChaosEffectInfo effect, EffectDispatchFlags dispatchFlags = EffectDispatchFlags.None)
        {
            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return;
            }

            dispatchEffect(effect, dispatchFlags);
        }

        BaseEffect dispatchEffect(ChaosEffectInfo effect, EffectDispatchFlags dispatchFlags = EffectDispatchFlags.None)
        {
            if (effect is null)
                throw new ArgumentNullException(nameof(effect));

            bool isServer = NetworkServer.active;
            if (!isServer && !effect.IsNetworked)
            {
                Log.Error($"Attempting to dispatch non-networked effect {effect} as client");
                return null;
            }

            if (isServer)
            {
                if ((dispatchFlags & EffectDispatchFlags.DontSendChatMessage) == 0)
                {
                    Chat.SendBroadcastChat(new Chat.SimpleChatMessage
                    {
                        baseToken = "CHAOS_EFFECT_ACTIVATE",
                        paramTokens = new string[] { effect.GetDisplayName() }
                    });
                }

                bool canActivate = (dispatchFlags & EffectDispatchFlags.CheckCanActivate) == 0 || effect.CanActivate(EffectCanActivateContext.Now);

                OnEffectAboutToDispatchServer?.Invoke(effect, dispatchFlags, canActivate);

                if (!canActivate)
                {
#if DEBUG
                    Log.Debug($"{effect} is not activatable, not starting");
#endif

                    return null;
                }
            }

            CreateEffectInstanceArgs createEffectArgs;
            if (isServer)
            {
                createEffectArgs = new CreateEffectInstanceArgs(_effectDispatchCount, _effectRNG.nextUlong);

                if ((dispatchFlags & EffectDispatchFlags.DontCount) == 0)
                {
                    _effectDispatchCount++;
                }
            }
            else
            {
                // Clients will get the seed from the server in Deserialize
                createEffectArgs = CreateEffectInstanceArgs.None;
            }

            BaseEffect effectInstance = effect.CreateInstance(createEffectArgs);
            if (effectInstance != null)
            {
                if (isServer)
                {
                    try
                    {
                        effectInstance.OnPreStartServer();
                    }
                    catch (Exception ex)
                    {
                        Log.Error_NoCallerPrefix($"Caught exception in {effect} {nameof(BaseEffect.OnPreStartServer)}: {ex}");
                        Chat.AddMessage(Language.GetString("CHAOS_EFFECT_UNHANDLED_EXCEPTION_MESSAGE"));
                    }

                    if (effect.IsNetworked)
                    {
                        NetworkWriter networkWriter = new NetworkWriter();

                        try
                        {
                            effectInstance.Serialize(networkWriter);
                        }
                        catch (Exception ex)
                        {
                            Log.Error_NoCallerPrefix($"Caught exception in {effect} {nameof(BaseEffect.Serialize)}: {ex}");
                        }

                        new NetworkedEffectDispatchedMessage(effect, dispatchFlags, networkWriter.ToArray()).Send(NetworkDestination.Clients);
                    }
                }

                if ((dispatchFlags & EffectDispatchFlags.DontStart) == 0)
                {
                    startEffect(effect, dispatchFlags, effectInstance);
                }
            }

            return effectInstance;
        }

        void startEffect(ChaosEffectInfo effectInfo, EffectDispatchFlags dispatchFlags, BaseEffect effectInstance)
        {
            try
            {
                effectInstance.OnStart();
            }
            catch (Exception ex)
            {
                Log.Error_NoCallerPrefix($"Caught exception in {effectInfo} {nameof(BaseEffect.OnStart)}: {ex}");
                Chat.AddMessage(Language.GetString("CHAOS_EFFECT_UNHANDLED_EXCEPTION_MESSAGE"));
            }

            OnEffectDispatched?.Invoke(effectInfo, dispatchFlags, effectInstance);
        }
    }
}
