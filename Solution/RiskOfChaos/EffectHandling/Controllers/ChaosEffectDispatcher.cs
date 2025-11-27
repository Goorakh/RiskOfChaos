using RiskOfChaos.ChatMessages;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.SaveHandling;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling.Controllers
{
    [DisallowMultipleComponent]
    public sealed class ChaosEffectDispatcher : NetworkBehaviour
    {
        static ChaosEffectDispatcher _instance;
        public static ChaosEffectDispatcher Instance => _instance;

        public delegate void EffectAboutToDispatchDelegate(ChaosEffectInfo effectInfo, in ChaosEffectDispatchArgs args, ref bool willStart);
        public event EffectAboutToDispatchDelegate OnEffectAboutToDispatchServer;

        [SerializedMember("rng")]
        Xoroshiro128Plus _effectRNG;

        void OnEnable()
        {
            SingletonHelper.Assign(ref _instance, this);

            if (NetworkServer.active)
            {
                ChaosEffectActivationSignaler.SignalShouldDispatchEffect += signalerDispatchEffect;

                _effectRNG = new Xoroshiro128Plus(Run.instance.seed);
            }
        }

        void Update()
        {
            if (NetworkServer.active)
            {
                tryActivateShortcutEffects();
            }
        }

        void OnDisable()
        {
            SingletonHelper.Unassign(ref _instance, this);

            ChaosEffectActivationSignaler.SignalShouldDispatchEffect -= signalerDispatchEffect;

            _effectRNG = null;
        }

        [Server]
        void tryActivateShortcutEffects()
        {
            if ((PauseStopController.instance && PauseStopController.instance.isPaused) || Time.deltaTime == 0f || InputUtils.IsUsingInputField())
                return;

            foreach (ChaosEffectInfo effectInfo in ChaosEffectCatalog.AllEffects)
            {
                if (effectInfo.IsActivationShortcutPressed)
                {
                    shortcutDispatchEffect(effectInfo.EffectIndex);
                    break;
                }
            }
        }

        [ConCommand(commandName = "roc_startrandom", flags = ConVarFlagUtil.SERVER, helpText = "Dispatches a random effect")]
        static void CCDispatchRandomEffect(ConCommandArgs args)
        {
            if (!NetworkServer.active || !Run.instance || !_instance || !_instance.enabled)
                return;

            ChaosEffectInfo effectInfo = ChaosEffectCatalog.PickActivatableEffect(RoR2Application.rng, EffectCanActivateContext.Now);
            if (effectInfo == null)
                return;

            _instance.consoleDispatchEffect(effectInfo.EffectIndex, new ChaosEffectDispatchArgs
            {
                RNGSeed = RoR2Application.rng.nextUlong
            });
        }

        [ConCommand(commandName = "roc_start", flags = ConVarFlagUtil.SERVER, helpText = "Dispatches an effect")]
        static void CCDispatchEffect(ConCommandArgs args)
        {
            if (!NetworkServer.active || !Run.instance || !_instance || !_instance.enabled)
                return;

            ChaosEffectIndex effectIndex = args.GetArgChaosEffectIndex(0);
            _instance.consoleDispatchEffect(effectIndex, new ChaosEffectDispatchArgs
            {
                RNGSeed = args.Count > 1 ? args.GetArgULong(1) : RoR2Application.rng.nextUlong
            });
        }

        [Server]
        void shortcutDispatchEffect(ChaosEffectIndex effectIndex)
        {
            ChaosEffectInfo effectInfo = ChaosEffectCatalog.GetEffectInfo(effectIndex);
            if (effectInfo == null)
                return;

            if (effectInfo.CanActivate(EffectCanActivateContext.Now_Shortcut))
            {
                DispatchEffectServer(effectInfo, new ChaosEffectDispatchArgs
                {
                    RNGSeed = RoR2Application.rng.nextUlong
                });
            }
            else
            {
                if (ChaosEffectActivationSoundHandler.Instance)
                {
                    ChaosEffectActivationSoundHandler.Instance.PlayEffectActivatedSoundServer();
                }

                Chat.AddMessage(new ChaosEffectChatMessage("CHAOS_EFFECT_SHORTCUT_CANNOT_ACTIVATE", effectInfo.EffectIndex, EffectNameFormatFlags.RuntimeFormatArgs));
            }
        }

        [Server]
        void consoleDispatchEffect(ChaosEffectIndex effectIndex, ChaosEffectDispatchArgs args)
        {
            ChaosEffectInfo effectInfo = ChaosEffectCatalog.GetEffectInfo(effectIndex);
            if (effectInfo != null)
            {
                DispatchEffectServer(effectInfo, args);
            }
        }

        [Server]
        void signalerDispatchEffect(ChaosEffectActivationSignaler signaler, ChaosEffectInfo effect, in ChaosEffectDispatchArgs args = default)
        {
            if (Configs.General.DisableEffectDispatching.Value)
                return;

            ChaosEffectDispatchArgs dispatchArgs = args;
            dispatchArgs.RNGSeed = _effectRNG.nextUlong;
            DispatchEffectServer(effect, dispatchArgs);
        }

        [Server]
        public ChaosEffectComponent DispatchEffectServer(ChaosEffectInfo effect, in ChaosEffectDispatchArgs dispatchArgs)
        {
            if ((dispatchArgs.DispatchFlags & EffectDispatchFlags.DontSendChatMessage) == 0)
            {
                Chat.SendBroadcastChat(new ChaosEffectChatMessage("CHAOS_EFFECT_ACTIVATE", effect.EffectIndex, EffectNameFormatFlags.All));
            }

            bool canActivate = (dispatchArgs.DispatchFlags & EffectDispatchFlags.CheckCanActivate) == 0 || effect.CanActivate(EffectCanActivateContext.Now);

            OnEffectAboutToDispatchServer?.Invoke(effect, dispatchArgs, ref canActivate);

            if (!canActivate)
            {
                Log.Debug($"{effect} is not activatable, not dispatching");
                return null;
            }

            return dispatchEffect(effect, dispatchArgs);
        }

        [Server]
        ChaosEffectComponent dispatchEffect(ChaosEffectInfo effect, in ChaosEffectDispatchArgs args)
        {
            if (effect is null)
                throw new ArgumentNullException(nameof(effect));

            GameObject effectController = null;

            try
            {
                effectController = Instantiate(effect.ControllerPrefab);

                ChaosEffectComponent effectComponent = effectController.GetComponent<ChaosEffectComponent>();

                if (effectComponent)
                {
                    if (effectComponent.ChaosEffectIndex != effect.EffectIndex)
                    {
                        throw new ArgumentException($"Effect prefab {effect.ControllerPrefab} is missing expected effect index, expected: {effect.EffectIndex}, set: {effectComponent.ChaosEffectIndex}");
                    }

                    RunTimeStamp startTime = args.OverrideStartTime ?? Run.FixedTimeStamp.now;

                    effectComponent.TimeStarted = startTime;

                    effectComponent.SetRngSeedServer(args.RNGSeed);
                }

                if (effect is TimedEffectInfo timedEffect)
                {
                    if (effectComponent.DurationComponent)
                    {
                        TimedEffectType timedType = args.OverrideDurationType ?? timedEffect.TimedType;
                        float duration = args.OverrideDuration ?? timedEffect.GetDuration(timedType);

                        effectComponent.DurationComponent.TimedType = timedType;
                        effectComponent.DurationComponent.Duration = duration;
                    }
                    else
                    {
                        Log.Error($"Timed effect {timedEffect} is missing duration component");
                    }
                }

                NetworkServer.Spawn(effectController);

                return effectComponent;
            }
            catch (Exception e)
            {
                Log.Error_NoCallerPrefix($"Caught exception instantiating effect {effect}: {e}");
                Chat.AddMessage(Language.GetString("CHAOS_EFFECT_UNHANDLED_EXCEPTION_MESSAGE"));

                if (effectController)
                {
                    Destroy(effectController);
                }
            }

            return null;
        }
    }
}
