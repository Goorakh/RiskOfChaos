using UnityEngine;

namespace RiskOfChaos.EffectHandling.Controllers
{
    public abstract class ChaosEffectActivationSignaler_ChatVote : MonoBehaviour, IChaosEffectActivationSignaler
    {
        public abstract event IChaosEffectActivationSignaler.SignalShouldDispatchEffectDelegate SignalShouldDispatchEffect;

        public abstract void SkipAllScheduledEffects();

        protected void onChatMessageReceived(string userId, string message)
        {

        }
    }
}
