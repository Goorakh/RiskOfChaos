using RiskOfChaos.EffectDefinitions;
using RoR2;
using System.Collections.Generic;
using System.Linq;
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
            if (Configs.EffectSelection.SeededEffectSelection.Value)
            {
                dispatchArgs = new ChaosEffectDispatchArgs
                {
                    DispatchFlags = EffectDispatchFlags.CheckCanActivate,
                    OverrideRNGSeed = rng.nextUlong
                };

                return ChaosEffectCatalog.PickEnabledEffect(rng, excludeEffects);
            }
            else
            {
                dispatchArgs = new ChaosEffectDispatchArgs();
                return ChaosEffectCatalog.PickActivatableEffect(rng, EffectCanActivateContext.Now, excludeEffects);
            }
        }

        public static ChaosEffectInfo PickEffectFromList(Xoroshiro128Plus rng, IEnumerable<ChaosEffectInfo> pickableEffects, out ChaosEffectDispatchArgs dispatchArgs)
        {
            if (Configs.EffectSelection.SeededEffectSelection.Value)
            {
                dispatchArgs = new ChaosEffectDispatchArgs
                {
                    DispatchFlags = EffectDispatchFlags.CheckCanActivate,
                    OverrideRNGSeed = rng.nextUlong
                };
            }
            else
            {
                dispatchArgs = new ChaosEffectDispatchArgs();

                pickableEffects = pickableEffects.Where(e => e.CanActivate(EffectCanActivateContext.Now));
            }

            if (pickableEffects.Any())
            {
                WeightedSelection<ChaosEffectInfo> effectSelector = new WeightedSelection<ChaosEffectInfo>();
                foreach (ChaosEffectInfo effect in pickableEffects)
                {
                    effectSelector.AddChoice(effect, effect.TotalSelectionWeight);
                }

                return effectSelector.Evaluate(rng.nextNormalizedFloat);
            }
            else
            {
                Log.Warning("No effect was activatable, defaulting to Nothing");
                return Nothing.EffectInfo;
            }
        }

        public static ChaosEffectInfo PickEffectFromList(Xoroshiro128Plus rng, IEnumerable<OverrideEffect> overrideEffects, out ChaosEffectDispatchArgs dispatchArgs)
        {
            if (Configs.EffectSelection.SeededEffectSelection.Value)
            {
                dispatchArgs = new ChaosEffectDispatchArgs
                {
                    DispatchFlags = EffectDispatchFlags.CheckCanActivate,
                    OverrideRNGSeed = rng.nextUlong
                };
            }
            else
            {
                dispatchArgs = new ChaosEffectDispatchArgs();

                overrideEffects = overrideEffects.Where(e => e.Effect.CanActivate(EffectCanActivateContext.Now));
            }

            if (overrideEffects.Any())
            {
                WeightedSelection<ChaosEffectInfo> effectSelector = new WeightedSelection<ChaosEffectInfo>();
                foreach (OverrideEffect overrideEffect in overrideEffects)
                {
                    effectSelector.AddChoice(overrideEffect.Effect, overrideEffect.GetWeight());
                }

                return effectSelector.Evaluate(rng.nextNormalizedFloat);
            }
            else
            {
                Log.Warning("No effect was activatable, defaulting to Nothing");
                return Nothing.EffectInfo;
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

        public abstract float GetTimeUntilNextEffect();

        public virtual ChaosEffectIndex GetUpcomingEffect()
        {
            return ChaosEffectIndex.Invalid;
        }
    }
}
