using RoR2;
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

        public override void SkipAllScheduledEffects()
        {
            _effectDispatchTimer?.SkipAllScheduledActivations();
        }

        void OnEnable()
        {
            Configs.General.OnTimeBetweenEffectsChanged += onTimeBetweenEffectsConfigChanged;

            _effectDispatchTimer = new CompletePeriodicRunTimer(Configs.General.TimeBetweenEffects);
            _effectDispatchTimer.OnActivate += dispatchRandomEffect;

            if (Run.instance)
            {
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
