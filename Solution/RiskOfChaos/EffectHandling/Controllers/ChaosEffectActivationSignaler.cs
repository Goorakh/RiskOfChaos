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
        public const float MIN_STAGE_TIME_REQUIRED_TO_DISPATCH = 2f;

        public delegate void SignalShouldDispatchEffectDelegate(ChaosEffectInfo effect, in ChaosEffectDispatchArgs args = default);
        public abstract event SignalShouldDispatchEffectDelegate SignalShouldDispatchEffect;

        public delegate bool CanDispatchEffectsOverrideDelegate();
        public static event CanDispatchEffectsOverrideDelegate CanDispatchEffectsOverride;

        public static bool EffectDispatchingTemporarilyDisabled
        {
            get
            {
                if (PauseStopController.instance && PauseStopController.instance.isPaused)
                    return true;

                if (SceneExitController.isRunning)
                    return true;

                if (!Stage.instance || Stage.instance.entryTime.timeSince < MIN_STAGE_TIME_REQUIRED_TO_DISPATCH)
                    return true;

                return false;
            }
        }

        public static bool EffectDispatchingCompletelyDisabled
        {
            get
            {
                if (!NetworkServer.active)
                    return true;

                Run run = Run.instance;
                if (!run || run.isGameOverServer || (run.isRunStopwatchPaused && !Configs.General.RunEffectsTimerWhileRunTimerPaused.Value))
                    return true;

                Stage stage = Stage.instance;
                if (!stage)
                    return true;

                SceneDef currentScene = stage.sceneDef;
                if (!currentScene)
                    return true;

                switch (currentScene.sceneType)
                {
                    case SceneType.Menu:
                    case SceneType.Cutscene:
                        return true;
                    case SceneType.Invalid:
                        switch (currentScene.cachedName)
                        {
                            case "ai_test":
                            case "moon":
                                break;
                            default:
                                return true;
                        }

                        break;
                }

                if (CanDispatchEffectsOverride != null)
                {
                    foreach (CanDispatchEffectsOverrideDelegate canDispatchEffectsDelegate in CanDispatchEffectsOverride.GetInvocationList())
                    {
                        if (!canDispatchEffectsDelegate())
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        public static bool EffectDispatchingDisabled
        {
            get
            {
                if (EffectDispatchingCompletelyDisabled)
                    return true;

                if (EffectDispatchingTemporarilyDisabled)
                    return true;

                return false;
            }
        }

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

        protected virtual bool canDispatchEffects => !EffectDispatchingDisabled;

        public abstract float GetTimeUntilNextEffect();

        public virtual ChaosEffectIndex GetUpcomingEffect()
        {
            return ChaosEffectIndex.Invalid;
        }
    }
}
