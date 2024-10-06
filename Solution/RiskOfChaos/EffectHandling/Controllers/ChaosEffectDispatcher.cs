using RiskOfChaos.ChatMessages;
using RiskOfChaos.EffectDefinitions;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.ModifierController.Effect;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling.Controllers
{
    [DisallowMultipleComponent]
    public class ChaosEffectDispatcher : NetworkBehaviour
    {
        static ChaosEffectDispatcher _instance;
        public static ChaosEffectDispatcher Instance => _instance;

        public delegate void EffectAboutToDispatchDelegate(ChaosEffectInfo effectInfo, in ChaosEffectDispatchArgs args, ref bool willStart);
        public event EffectAboutToDispatchDelegate OnEffectAboutToDispatchServer;

        Xoroshiro128Plus _effectRNG;

        void OnEnable()
        {
            SingletonHelper.Assign(ref _instance, this);

            if (NetworkServer.active)
            {
                ChaosEffectActivationSignaler.SignalShouldDispatchEffect += onSignalerDispatchEffect;

                _effectRNG = new Xoroshiro128Plus(Run.instance.seed);
            }
        }

        void Update()
        {
            if (PauseStopController.instance && PauseStopController.instance.isPaused)
                return;

            if (!NetworkServer.active || InputUtils.IsUsingInputField())
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

        void OnDisable()
        {
            SingletonHelper.Unassign(ref _instance, this);

            ChaosEffectActivationSignaler.SignalShouldDispatchEffect -= onSignalerDispatchEffect;

            _effectRNG = null;
        }

        [Obsolete]
        [Server]
        public ChaosEffectActivationSignaler GetCurrentEffectSignaler()
        {
            return null;
        }

        [Server]
        [Obsolete]
        public void SkipAllScheduledEffects()
        {
        }

        [Server]
        [Obsolete]
        public void RewindEffectScheduling(float numSeconds)
        {
        }

        [ConCommand(commandName = "roc_startrandom", flags = ConVarFlags.SenderMustBeServer, helpText = "Dispatches a random effect")]
        static void CCDispatchRandomEffect(ConCommandArgs args)
        {
            if (!NetworkServer.active || !Run.instance || !_instance || !_instance.enabled)
                return;

            _instance.dispatchEffect(ChaosEffectCatalog.PickActivatableEffect(RoR2Application.rng, EffectCanActivateContext.Now), new ChaosEffectDispatchArgs
            {
                RNGSeed = RoR2Application.rng.nextUlong
            });
        }

        [ConCommand(commandName = "roc_start", flags = ConVarFlags.SenderMustBeServer, helpText = "Dispatches an effect")]
        static void CCDispatchEffect(ConCommandArgs args)
        {
            if (!NetworkServer.active || !Run.instance || !_instance || !_instance.enabled)
                return;

            ChaosEffectIndex effectIndex = args.GetArgChaosEffectIndex(0);
            _instance.dispatchEffect(ChaosEffectCatalog.GetEffectInfo(effectIndex), new ChaosEffectDispatchArgs
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
                    ChaosEffectActivationSoundHandler.Instance.PlayEffectActivationSound();
                }

                Chat.AddMessage(new ChaosEffectChatMessage("CHAOS_EFFECT_SHORTCUT_CANNOT_ACTIVATE", effectInfo, EffectNameFormatFlags.RuntimeFormatArgs).ConstructChatString());
            }
        }

        [Server]
        void onSignalerDispatchEffect(ChaosEffectActivationSignaler signaler, ChaosEffectInfo effect, in ChaosEffectDispatchArgs args = default)
        {
            if (Configs.General.DisableEffectDispatching.Value)
                return;

            ChaosEffectDispatchArgs dispatchArgs = args;
            dispatchArgs.RNGSeed = _effectRNG.nextUlong;
            DispatchEffectServer(effect, dispatchArgs);
        }

        [Server]
        public void DispatchEffectServer(ChaosEffectInfo effect, in ChaosEffectDispatchArgs dispatchArgs)
        {
            if ((dispatchArgs.DispatchFlags & EffectDispatchFlags.DontSendChatMessage) == 0)
            {
                Chat.SendBroadcastChat(new ChaosEffectChatMessage("CHAOS_EFFECT_ACTIVATE", effect, EffectNameFormatFlags.All));
            }

            bool canActivate = (dispatchArgs.DispatchFlags & EffectDispatchFlags.CheckCanActivate) == 0 || effect.CanActivate(EffectCanActivateContext.Now);

            OnEffectAboutToDispatchServer?.Invoke(effect, dispatchArgs, ref canActivate);

            if (!canActivate)
            {
#if DEBUG
                Log.Debug($"{effect} is not activatable, not dispatching");
#endif
                return;
            }

            dispatchEffect(effect, dispatchArgs);
        }

        [Server]
        void dispatchEffect(ChaosEffectInfo effect, in ChaosEffectDispatchArgs args)
        {
            if (effect is null)
                throw new ArgumentNullException(nameof(effect));

            GameObject effectController = null;

            try
            {
                TimedEffectInfo timedEffect = effect as TimedEffectInfo;

                effectController = Instantiate(effect.ControllerPrefab);

                if (effectController.TryGetComponent(out ChaosEffectComponent effectComponent))
                {
                    if (effectComponent.ChaosEffectIndex != effect.EffectIndex)
                    {
                        throw new ArgumentException($"Effect prefab {effect.ControllerPrefab} is missing expected effect index, expected: {effect.EffectIndex}, set: {effectComponent.ChaosEffectIndex}");
                    }

                    RunTimeStamp startTime = Run.FixedTimeStamp.now;
                    if (args.OverrideStartTime.HasValue)
                    {
                        startTime = args.OverrideStartTime.Value;
                    }

                    effectComponent.TimeStarted = startTime;

                    effectComponent.SetRngSeedServer(args.RNGSeed);
                }

                if (timedEffect != null)
                {
                    if (!effectController.TryGetComponent(out ChaosEffectDurationComponent durationComponent))
                    {
                        Log.Error($"Timed effect {timedEffect} is missing duration component");
                        Destroy(effectController);
                        return;
                    }

                    TimedEffectType timedType = timedEffect.TimedType;

                    if (args.OverrideDurationType.HasValue)
                    {
                        timedType = args.OverrideDurationType.Value;
                    }

                    float duration = timedEffect.GetDuration(timedType);

                    if (args.OverrideDuration.HasValue)
                    {
                        duration = args.OverrideDuration.Value;
                    }

                    durationComponent.TimedType = timedType;
                    durationComponent.Duration = duration;
                }

                NetworkServer.Spawn(effectController);
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
        }
    }
}
