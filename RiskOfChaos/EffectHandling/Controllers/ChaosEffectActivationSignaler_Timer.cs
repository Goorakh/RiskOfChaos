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
            Configs.General.OnTimeBetweenEffectsChanged += onTimeBetweenEffectsConfigChanged;

            _effectDispatchTimer = new CompletePeriodicRunTimer(Configs.General.TimeBetweenEffects);
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

                _nextEffectRNG = new Xoroshiro128Plus(Run.instance.runRNG.nextUlong);
            }
        }

        void OnDisable()
        {
            Configs.General.OnTimeBetweenEffectsChanged -= onTimeBetweenEffectsConfigChanged;

            if (_effectDispatchTimer != null)
            {
                _effectDispatchTimer.OnActivate -= dispatchRandomEffect;
                _effectDispatchTimer = null;
            }

            _nextEffectRNG = null;
        }

        void onTimeBetweenEffectsConfigChanged()
        {
            _effectDispatchTimer.Period = Configs.General.TimeBetweenEffects;
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

            SignalShouldDispatchEffect?.Invoke(ChaosEffectCatalog.PickActivatableEffect(_nextEffectRNG, EffectCanActivateContext.Now));
        }
    }
}
