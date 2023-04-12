using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling.Controllers
{
    public abstract class ChaosEffectActivationSignaler : MonoBehaviour
    {
        public delegate void SignalShouldDispatchEffectDelegate(in ChaosEffectInfo effect, EffectDispatchFlags dispatchFlags = EffectDispatchFlags.None);

        public abstract event SignalShouldDispatchEffectDelegate SignalShouldDispatchEffect;

        public abstract void SkipAllScheduledEffects();

        protected const float MIN_STAGE_TIME_REQUIRED_TO_DISPATCH = 2f;
        protected virtual bool canDispatchEffects
        {
            get
            {
                if (!NetworkServer.active)
                    return false;

                if (PauseManager.isPaused && NetworkServer.dontListen)
                    return false;

                if (SceneExitController.isRunning)
                    return false;

                if (!Run.instance || Run.instance.isGameOverServer)
                    return false;

                if (!Stage.instance || Stage.instance.entryTime.timeSince < MIN_STAGE_TIME_REQUIRED_TO_DISPATCH)
                    return false;

                return true;
            }
        }
    }
}
