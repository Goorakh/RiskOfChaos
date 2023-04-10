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

                const float STAGE_START_OFFSET = 2f;
                if (!Stage.instance || Stage.instance.entryTime.timeSince < STAGE_START_OFFSET)
                    return false;

                return true;
            }
        }
    }
}
