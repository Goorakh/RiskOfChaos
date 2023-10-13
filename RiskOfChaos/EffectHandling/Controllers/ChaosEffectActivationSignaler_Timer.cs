using RiskOfChaos.ConfigHandling;
using RiskOfChaos.SaveHandling;
using RiskOfChaos.SaveHandling.DataContainers;
using RiskOfChaos.SaveHandling.DataContainers.EffectHandlerControllers;
using RoR2;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling.Controllers
{
    [ChaosEffectActivationSignaler(Configs.ChatVoting.ChatVotingMode.Disabled)]
    public class ChaosEffectActivationSignaler_Timer : ChaosEffectActivationSignaler
    {
        public override event SignalShouldDispatchEffectDelegate SignalShouldDispatchEffect;

        CompletePeriodicRunTimer _effectDispatchTimer;
        Xoroshiro128Plus _nextEffectRNG;

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

                _nextEffectRNG = new Xoroshiro128Plus(Run.instance.seed);
            }

            if (SaveManager.UseSaveData)
            {
                SaveManager.CollectSaveData += SaveManager_CollectSaveData;
                SaveManager.LoadSaveData += SaveManager_LoadSaveData;
            }
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

        void dispatchRandomEffect()
        {
            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return;
            }

            SignalShouldDispatchEffect?.Invoke(PickEffect(_nextEffectRNG, out EffectDispatchFlags flags), flags);
        }
    }
}
