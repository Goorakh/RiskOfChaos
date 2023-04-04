using System;

namespace RiskOfChaos.EffectHandling.Controllers
{
    public interface IChaosEffectActivationSignaler
    {
        public delegate void SignalShouldDispatchEffectDelegate(in ChaosEffectInfo effect, EffectDispatchFlags dispatchFlags = EffectDispatchFlags.None);

        event SignalShouldDispatchEffectDelegate SignalShouldDispatchEffect;

        void SkipAllScheduledEffects();
    }
}
