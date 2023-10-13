using RoR2;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling.Controllers
{
    public abstract class ChaosEffectActivationSignaler : MonoBehaviour
    {
        public delegate void SignalShouldDispatchEffectDelegate(ChaosEffectInfo effect, in ChaosEffectDispatchArgs args = default);
        public abstract event SignalShouldDispatchEffectDelegate SignalShouldDispatchEffect;

        public abstract void SkipAllScheduledEffects();
        public abstract void RewindEffectScheduling(float numSeconds);

        public static ChaosEffectInfo PickEffect(Xoroshiro128Plus rng, out ChaosEffectDispatchArgs dispatchArgs)
        {
            return PickEffect(rng, null, out dispatchArgs);
        }

        public static ChaosEffectInfo PickEffect(Xoroshiro128Plus rng, HashSet<ChaosEffectInfo> excludeEffects, out ChaosEffectDispatchArgs dispatchArgs)
        {
            if (Configs.General.SeededEffectSelection.Value)
            {
                dispatchArgs = new ChaosEffectDispatchArgs
                {
                    DispatchFlags = EffectDispatchFlags.CheckCanActivate
                };

                return ChaosEffectCatalog.PickEffect(rng, excludeEffects);
            }
            else
            {
                dispatchArgs = new ChaosEffectDispatchArgs();
                return ChaosEffectCatalog.PickActivatableEffect(rng, EffectCanActivateContext.Now, excludeEffects);
            }
        }

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

                if (!Run.instance || Run.instance.isGameOverServer || (Run.instance.isRunStopwatchPaused && !Configs.General.RunEffectsTimerWhileRunTimerPaused.Value))
                    return false;

                if (!Stage.instance || Stage.instance.entryTime.timeSince < MIN_STAGE_TIME_REQUIRED_TO_DISPATCH)
                    return false;

                SceneDef currentScene = Stage.instance.sceneDef;
                if (!currentScene)
                    return false;

                switch (currentScene.sceneType)
                {
                    case SceneType.Stage:
                    case SceneType.Intermission:
                    case SceneType.Invalid when currentScene.cachedName == "ai_test" || currentScene.cachedName == "moon":
                        break;
                    default:
                        return false;
                }

                return true;
            }
        }
    }
}
