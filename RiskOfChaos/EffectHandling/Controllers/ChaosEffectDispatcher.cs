using R2API.Networking;
using R2API.Networking.Interfaces;
using RiskOfChaos.EffectDefinitions;
using RiskOfChaos.Networking;
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

        public delegate void EffectDispatchedDelegate(in ChaosEffectInfo effectInfo, EffectDispatchFlags dispatchFlags, BaseEffect effectInstance);
        public event EffectDispatchedDelegate OnEffectDispatched;

        public delegate void EffectAboutToDispatchDelegate(in ChaosEffectInfo effectInfo, EffectDispatchFlags dispatchFlags, bool willStart);
        public event EffectAboutToDispatchDelegate OnEffectAboutToDispatchServer;

        ChaosEffectActivationSignaler[] _effectActivationSignalers;

        Xoroshiro128Plus _effectRNG;

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
                    activationSignaler.SignalShouldDispatchEffect += DispatchEffect;
                }

                if (Run.instance)
                {
                    _effectRNG = new Xoroshiro128Plus(Run.instance.runRNG.nextUlong);
                }
            }

            NetworkedEffectDispatchedMessage.OnReceive += NetworkedEffectDispatchedMessage_OnReceive;
        }

        void OnDisable()
        {
            SingletonHelper.Unassign(ref _instance, this);

            foreach (ChaosEffectActivationSignaler activationSignaler in _effectActivationSignalers)
            {
                activationSignaler.SignalShouldDispatchEffect -= DispatchEffect;
            }

            _effectRNG = null;

            NetworkedEffectDispatchedMessage.OnReceive -= NetworkedEffectDispatchedMessage_OnReceive;
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
                if (activationSignaler is Behaviour behaviour)
                {
                    if (behaviour.enabled)
                    {
                        activationSignaler.SkipAllScheduledEffects();
                    }
                }
                else
                {
                    Log.Error($"Non-Behaviour type for effect signaler {activationSignaler}");
                }
            }
        }

        void NetworkedEffectDispatchedMessage_OnReceive(in ChaosEffectInfo effectInfo, EffectDispatchFlags dispatchFlags, byte[] serializedEffectData)
        {
            if (NetworkServer.active)
                return;

            BaseEffect effectInstance = dispatchEffect(effectInfo, dispatchFlags | EffectDispatchFlags.DontStart);
            if (effectInstance != null)
            {
                NetworkReader networkReader = new NetworkReader(serializedEffectData);

                try
                {
                    effectInstance.Deserialize(networkReader);
                }
                catch (Exception ex)
                {
                    Log.Error($"Caught exception in {effectInfo} {nameof(BaseEffect.Deserialize)}: {ex}");
                }

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

            _instance.dispatchEffect(ChaosEffectCatalog.PickActivatableEffect(RoR2Application.rng, EffectCanActivateContext.Now), EffectDispatchFlags.DontStopTimedEffects);
        }

        [ConCommand(commandName = "roc_start", flags = ConVarFlags.SenderMustBeServer, helpText = "Dispatches an effect")]
        static void CCDispatchEffect(ConCommandArgs args)
        {
            if (!NetworkServer.active || !Run.instance || !_instance || !_instance.enabled)
                return;

            int index = ChaosEffectCatalog.FindEffectIndex(args[0]);
            if (index >= 0)
            {
                _instance.dispatchEffect(ChaosEffectCatalog.GetEffectInfo((uint)index), EffectDispatchFlags.DontStopTimedEffects);
            }
        }

        public void DispatchEffect(in ChaosEffectInfo effect, EffectDispatchFlags dispatchFlags = EffectDispatchFlags.None)
        {
            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return;
            }

#if DEBUG
            if (Configs.Debug.DebugDisable)
                return;
#endif

            dispatchEffect(effect, dispatchFlags);
        }

        BaseEffect dispatchEffect(in ChaosEffectInfo effect, EffectDispatchFlags dispatchFlags = EffectDispatchFlags.None)
        {
            bool isServer = NetworkServer.active;
            if (!isServer && !effect.IsNetworked)
            {
                Log.Error($"Attempting to dispatch non-networked effect {effect} as client");
                return null;
            }

            if (isServer)
            {
                Chat.SendBroadcastChat(new Chat.SimpleChatMessage { baseToken = effect.GetActivationMessage() });

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

            ulong effectRNGSeed;
            if (isServer)
            {
                effectRNGSeed = _effectRNG.nextUlong;
            }
            else
            {
                // Clients will get the seed from the server in Deserialize
                effectRNGSeed = 0UL;
            }

            BaseEffect effectInstance = effect.InstantiateEffect(effectRNGSeed);
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
                        Log.Error($"Caught exception in {effect} {nameof(BaseEffect.OnPreStartServer)}: {ex}");
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
                            Log.Error($"Caught exception in {effect} {nameof(BaseEffect.Serialize)}: {ex}");
                        }

                        new NetworkedEffectDispatchedMessage(effect, dispatchFlags, networkWriter.AsArray()).Send(NetworkDestination.Clients);
                    }
                }

                if ((dispatchFlags & EffectDispatchFlags.DontStart) == 0)
                {
                    startEffect(effect, dispatchFlags, effectInstance);
                }
            }

            return effectInstance;
        }

        void startEffect(in ChaosEffectInfo effectInfo, EffectDispatchFlags dispatchFlags, BaseEffect effectInstance)
        {
            try
            {
                effectInstance.OnStart();
            }
            catch (Exception ex)
            {
                Log.Error($"Caught exception in {effectInfo} {nameof(BaseEffect.OnStart)}: {ex}");
            }

            OnEffectDispatched?.Invoke(effectInfo, dispatchFlags, effectInstance);
        }
    }
}
