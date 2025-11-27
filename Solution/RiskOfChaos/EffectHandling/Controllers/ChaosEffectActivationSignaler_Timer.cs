using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectDefinitions;
using RiskOfChaos.SaveHandling;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling.Controllers
{
    public sealed class ChaosEffectActivationSignaler_Timer : ChaosEffectActivationSignaler
    {
        CompletePeriodicRunTimer _effectDispatchTimer;

        Xoroshiro128Plus _nextEffectRNG;

        [SerializedMember("rng")]
        Xoroshiro128Plus serialized_nextEffectRng
        {
            get => _nextEffectRNG;
            set
            {
                _nextEffectRNG = value;
                updateNextEffect();
            }
        }

        [SerializedMember("leat")]
        float serialized_lastEffectActivationTimeStopwatch
        {
            get
            {
                if (!enabled)
                    return 0f;

                if (_effectDispatchTimer == null)
                    return 0f;

                return _effectDispatchTimer.GetLastActivationTimeStopwatch().Time;
            }
            set
            {
                if (!enabled)
                    return;

                if (_effectDispatchTimer == null)
                {
                    Log.Error("Failed to set last activation time, no timer instance");
                    return;
                }

                _effectDispatchTimer.SetLastActivationTimeStopwatch(value);

                Log.Debug($"Loaded timer data, remaining={_effectDispatchTimer.GetNextActivationTime().TimeUntil}");
            }
        }

        OverrideEffect[] _overrideAvailableEffects;

        ChaosEffectIndex _nextEffectIndex = ChaosEffectIndex.Invalid;

        public override void SkipAllScheduledEffects()
        {
            _effectDispatchTimer?.SkipAllScheduledActivations();
        }

        public override void RewindEffectScheduling(float numSeconds)
        {
            _effectDispatchTimer?.RewindScheduledActivations(numSeconds);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            Configs.General.TimeBetweenEffects.SettingChanged += onTimeBetweenEffectsConfigChanged;
            Configs.EffectSelection.SeededEffectSelection.SettingChanged += onSeededEffectSelectionConfigChanged;

            _effectDispatchTimer = new CompletePeriodicRunTimer(Configs.General.TimeBetweenEffects.Value);
            _effectDispatchTimer.OnActivate += dispatchRandomEffect;

            if (Run.instance)
            {
                Log.Debug($"Run stopwatch: {Run.instance.GetRunStopwatch()}");

                if (Run.instance.GetRunStopwatch() > MIN_STAGE_TIME_REQUIRED_TO_DISPATCH)
                {
                    Log.Debug($"Skipping {_effectDispatchTimer.GetNumScheduledActivations()} effect activation(s)");

                    _effectDispatchTimer.SkipAllScheduledActivations();
                }

                _nextEffectRNG = new Xoroshiro128Plus(Run.instance.stageRng);
            }

            Stage.onStageStartGlobal += onStageStart;

            updateNextEffect();
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            Configs.General.TimeBetweenEffects.SettingChanged -= onTimeBetweenEffectsConfigChanged;
            Configs.EffectSelection.SeededEffectSelection.SettingChanged -= onSeededEffectSelectionConfigChanged;

            if (_effectDispatchTimer != null)
            {
                _effectDispatchTimer.OnActivate -= dispatchRandomEffect;
                _effectDispatchTimer = null;
            }

            _nextEffectRNG = null;

            Stage.onStageStartGlobal -= onStageStart;
        }

        void onStageStart(Stage stage)
        {
            if (!NetworkServer.active)
                return;

            if (Configs.EffectSelection.PerStageEffectListEnabled.Value)
            {
                if (stage.sceneDef.sceneType == SceneType.Stage)
                {
                    _nextEffectRNG = new Xoroshiro128Plus(Run.instance.stageRng);

                    setupAvailableEffectsList();
                    updateNextEffect();
                }
            }
            else
            {
                _overrideAvailableEffects = null;
            }
        }

        void setupAvailableEffectsList()
        {
            Xoroshiro128Plus rng = new Xoroshiro128Plus(Run.instance.stageRng);

            int effectsListSize = Configs.EffectSelection.PerStageEffectListSize?.Value ?? -1;
            if (effectsListSize <= 0)
            {
                Log.Error($"Invalid effect list size: {effectsListSize}, per-stage effects will not be used");
                _overrideAvailableEffects = null;
            }

            ChaosEffectInfo[] enabledEffects = [.. ChaosEffectCatalog.AllEffects.Where(e => e.IsEnabled())];
            if (enabledEffects.Length <= 0)
            {
                Log.Warning("No effects enabled, per-stage effect list cannot be generated");
                _overrideAvailableEffects = [ new OverrideEffect(Nothing.EffectInfo, null) ];
                return;
            }

            if (enabledEffects.Length < effectsListSize)
            {
                Log.Info($"Cannot generate effect list of size {effectsListSize}, only {enabledEffects.Length} effects available. Effect list of size {enabledEffects.Length} will be generated instead");
                effectsListSize = enabledEffects.Length;
            }

            Util.ShuffleArray(enabledEffects, rng.Branch());

            _overrideAvailableEffects = new OverrideEffect[effectsListSize];

            for (int i = 0; i < effectsListSize; i++)
            {
                _overrideAvailableEffects[i] = new OverrideEffect(enabledEffects[i], null);
            }

            Log.Debug($"Available effects: [{string.Join(", ", _overrideAvailableEffects)}]");
        }

        void onSeededEffectSelectionConfigChanged(object sender, ConfigChangedArgs<bool> e)
        {
            updateNextEffect();
        }

        void onTimeBetweenEffectsConfigChanged(object s, ConfigChangedArgs<float> args)
        {
            _effectDispatchTimer.Period = args.NewValue;
        }

        void Update()
        {
            if (!CanDispatchEffects)
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

        void dispatchRandomEffect(RunTimeStamp activationTime)
        {
            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return;
            }

            ChaosEffectInfo effect = pickNextEffect(_nextEffectRNG.Branch(), out ChaosEffectDispatchArgs args);
            args.OverrideStartTime = activationTime + Mathf.Round(activationTime.TimeSinceClamped);
            signalEffectDispatch(effect, args);

            updateNextEffect();
        }

        ChaosEffectIndex getNextEffect()
        {
            if (!Configs.EffectSelection.SeededEffectSelection.Value)
                return ChaosEffectIndex.Invalid;

            if (_nextEffectRNG is null)
                return ChaosEffectIndex.Invalid;

            Xoroshiro128Plus rngCopy = new Xoroshiro128Plus(_nextEffectRNG);

            ChaosEffectInfo nextEffect = pickNextEffect(rngCopy.Branch(), out _);
            if (nextEffect is null)
                return ChaosEffectIndex.Invalid;

            return nextEffect.EffectIndex;
        }

        void updateNextEffect()
        {
            _nextEffectIndex = getNextEffect();
        }

        public override RunTimeStamp GetNextEffectActivationTime()
        {
            return _effectDispatchTimer.GetNextActivationTime();
        }

        public override ChaosEffectIndex GetUpcomingEffect()
        {
            return _nextEffectIndex;
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
                    Debug.Log($"Removed '{ChaosEffectCatalog.GetEffectInfo(effectIndex).GetStaticDisplayName(EffectNameFormatFlags.RuntimeFormatArgs)}' from available effects list");

                    updateNextEffect();

                    return;
                }
            }

            Debug.Log($"{ChaosEffectCatalog.GetEffectInfo(effectIndex).GetStaticDisplayName(EffectNameFormatFlags.RuntimeFormatArgs)} is not in available stage effects");
        }

        [ConCommand(commandName = "roc_remove_stage_effect", flags = ConVarFlagUtil.SERVER, helpText = "Removes an effect from the current stage effect pool")]
        static void CCRemoveStageEffect(ConCommandArgs args)
        {
            ChaosEffectDispatcher dispatcher = ChaosEffectDispatcher.Instance;
            if (!dispatcher)
                return;

            ChaosEffectIndex effectIndex = args.GetArgChaosEffectIndex(0);

            bool removed = false;

            foreach (ChaosEffectActivationSignaler effectActivationSignaler in InstancesList)
            {
                if (effectActivationSignaler is ChaosEffectActivationSignaler_Timer timerEffectSignaler)
                {
                    timerEffectSignaler.tryRemoveStageEffect(effectIndex);
                    removed = true;
                }
            }

            if (!removed)
            {
                Debug.Log("Current effect mode does not support stage effects");
            }
        }
    }
}
