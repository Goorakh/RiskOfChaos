using HG;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectDefinitions;
using RiskOfChaos.SaveHandling;
using RiskOfChaos.SaveHandling.DataContainers;
using RiskOfChaos.SaveHandling.DataContainers.EffectHandlerControllers;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling.Controllers
{
    [ChaosEffectActivationSignaler(Configs.ChatVoting.ChatVotingMode.Disabled)]
    public class ChaosEffectActivationSignaler_Timer : ChaosEffectActivationSignaler
    {
        public override event SignalShouldDispatchEffectDelegate SignalShouldDispatchEffect;

        CompletePeriodicRunTimer _effectDispatchTimer;
        Xoroshiro128Plus _nextEffectRNG;

        OverrideEffect[] _overrideAvailableEffects;

        public override void SkipAllScheduledEffects()
        {
            _effectDispatchTimer?.SkipAllScheduledActivations();
        }

        public override void RewindEffectScheduling(float numSeconds)
        {
            _effectDispatchTimer?.RewindScheduledActivations(numSeconds);
        }

        void OnEnable()
        {
            Configs.General.TimeBetweenEffects.SettingChanged += onTimeBetweenEffectsConfigChanged;

            _effectDispatchTimer = new CompletePeriodicRunTimer(Configs.General.TimeBetweenEffects.Value);
            _effectDispatchTimer.OnActivate += dispatchRandomEffect;

            if (Run.instance)
            {
#if DEBUG
                Log.Debug($"Run stopwatch: {Run.instance.GetRunStopwatch()}");
#endif
                if (Run.instance.GetRunStopwatch() > MIN_STAGE_TIME_REQUIRED_TO_DISPATCH)
                {
#if DEBUG
                    Log.Debug($"Skipping {_effectDispatchTimer.GetNumScheduledActivations()} effect activation(s)");
#endif

                    _effectDispatchTimer.SkipAllScheduledActivations();
                }

                _nextEffectRNG = new Xoroshiro128Plus(Run.instance.stageRng);
            }

            if (SaveManager.UseSaveData)
            {
                SaveManager.CollectSaveData += SaveManager_CollectSaveData;
                SaveManager.LoadSaveData += SaveManager_LoadSaveData;
            }

            Stage.onStageStartGlobal += onStageStart;
        }

        void OnDisable()
        {
            Configs.General.TimeBetweenEffects.SettingChanged -= onTimeBetweenEffectsConfigChanged;

            if (_effectDispatchTimer != null)
            {
                _effectDispatchTimer.OnActivate -= dispatchRandomEffect;
                _effectDispatchTimer = null;
            }

            _nextEffectRNG = null;

            SaveManager.CollectSaveData -= SaveManager_CollectSaveData;
            SaveManager.LoadSaveData -= SaveManager_LoadSaveData;

            Stage.onStageStartGlobal -= onStageStart;
        }

        void onStageStart(Stage stage)
        {
            if (!NetworkServer.active)
                return;

            if (stage.sceneDef.sceneType == SceneType.Stage)
            {
                setupAvailableEffectsList();

                if (Configs.EffectSelection.PerStageEffectListEnabled.Value)
                {
                    _nextEffectRNG = new Xoroshiro128Plus(Run.instance.stageRng);
                }
            }
        }

        void setupAvailableEffectsList()
        {
            _overrideAvailableEffects = null;
            if (Configs.EffectSelection.PerStageEffectListEnabled.Value)
            {
                Xoroshiro128Plus rng = new Xoroshiro128Plus(Run.instance.stageRng);

                int effectsListSize = Configs.EffectSelection.PerStageEffectListSize.Value;

                WeightedSelection<ChaosEffectInfo> effectSelection = ChaosEffectCatalog.GetAllEnabledEffects();

                if (effectSelection.Count < effectsListSize)
                {
                    Log.Warning($"Cannot generate effect list of size {effectsListSize}, only {effectSelection.Count} effects available. Effect list of size {effectSelection.Count} will be generated instead");
                    effectsListSize = effectSelection.Count;
                }

                if (effectsListSize <= 0)
                {
                    Log.Error($"Invalid effect list size: {effectsListSize}, per-stage effects will not be used");
                    return;
                }

                _overrideAvailableEffects = new OverrideEffect[effectsListSize];

                for (int i = 0; i < effectsListSize; i++)
                {
                    _overrideAvailableEffects[i] = new OverrideEffect(effectSelection.GetAndRemoveRandom(new Xoroshiro128Plus(rng.nextUlong)), null);
                }

#if DEBUG
                Log.Debug($"Available effects: [{string.Join(", ", _overrideAvailableEffects)}]");
#endif
            }
        }

        void SaveManager_LoadSaveData(in SaveContainer container)
        {
            EffectActivationSignalerData data = container.ActivationSignalerData;

            _nextEffectRNG = data.NextEffectRng;
            _effectDispatchTimer.SetLastActivationTimeStopwatch(data.LastEffectActivationTime);

#if DEBUG
            Log.Debug($"Loaded timer data, remaining={_effectDispatchTimer.GetTimeRemaining()}");
#endif
        }

        void SaveManager_CollectSaveData(ref SaveContainer container)
        {
            container.ActivationSignalerData = new EffectActivationSignalerData
            {
                NextEffectRng = new SerializableRng(_nextEffectRNG),
                LastEffectActivationTime = _effectDispatchTimer.GetLastActivationTimeStopwatch()
            };
        }

        void onTimeBetweenEffectsConfigChanged(object s, ConfigChangedArgs<float> args)
        {
            _effectDispatchTimer.Period = args.NewValue;
        }

        void Update()
        {
            if (!canDispatchEffects)
                return;

            _effectDispatchTimer.Update();
        }

        ChaosEffectInfo pickNextEffect(Xoroshiro128Plus rng, out ChaosEffectDispatchArgs args)
        {
            if (_overrideAvailableEffects != null)
            {
                return PickEffectFromList(rng, _overrideAvailableEffects, out args);
            }
            else
            {
                return PickEffect(rng, out args);
            }
        }

        void dispatchRandomEffect()
        {
            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return;
            }

            ChaosEffectInfo effect = pickNextEffect(new Xoroshiro128Plus(_nextEffectRNG.nextUlong), out ChaosEffectDispatchArgs args);
            SignalShouldDispatchEffect?.Invoke(effect, args);
        }

        public override float GetTimeUntilNextEffect()
        {
            return Mathf.Max(0f, _effectDispatchTimer.GetTimeRemaining());
        }

        public override ChaosEffectIndex GetUpcomingEffect()
        {
            if (!Configs.EffectSelection.SeededEffectSelection.Value)
                return ChaosEffectIndex.Invalid;

            if (_nextEffectRNG is null)
                return ChaosEffectIndex.Invalid;

            Xoroshiro128Plus rngCopy = new Xoroshiro128Plus(_nextEffectRNG);
            return pickNextEffect(new Xoroshiro128Plus(rngCopy.nextUlong), out _)?.EffectIndex ?? ChaosEffectIndex.Invalid;
        }

        void tryRemoveStageEffect(ChaosEffectIndex effectIndex)
        {
            if (_overrideAvailableEffects == null)
            {
                Debug.Log("Per-stage effects selection not active");
                return;
            }

            for (int i = 0; i < _overrideAvailableEffects.Length; i++)
            {
                if (_overrideAvailableEffects[i].Effect.EffectIndex == effectIndex)
                {
                    _overrideAvailableEffects[i] = new OverrideEffect(Nothing.EffectInfo, _overrideAvailableEffects[i].GetWeight());
                    Debug.Log($"Removed '{ChaosEffectCatalog.GetEffectInfo(effectIndex).GetDisplayName(EffectNameFormatFlags.None)}' from available effects list");

                    return;
                }
            }

            Debug.Log($"{ChaosEffectCatalog.GetEffectInfo(effectIndex).GetDisplayName(EffectNameFormatFlags.None)} is not in available stage effects");
        }

        [ConCommand(commandName = "roc_remove_stage_effect", flags = ConVarFlags.SenderMustBeServer, helpText = "Removes an effect from the current stage effect pool")]
        static void CCRemoveStageEffect(ConCommandArgs args)
        {
            ChaosEffectDispatcher dispatcher = ChaosEffectDispatcher.Instance;
            if (!dispatcher)
                return;

            ChaosEffectActivationSignaler effectSignaler = dispatcher.GetCurrentEffectSignaler();
            if (!effectSignaler)
                return;

            ChaosEffectIndex effectIndex = args.GetArgChaosEffectIndex(0);

            if (effectSignaler is ChaosEffectActivationSignaler_Timer timerEffectSignaler)
            {
                timerEffectSignaler.tryRemoveStageEffect(effectIndex);
            }
            else
            {
                Debug.Log("Current effect mode does not support stage effects");
            }
        }
    }
}
