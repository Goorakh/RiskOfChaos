using RiskOfChaos.EffectDefinitions.World.RunTimer;
using RiskOfChaos.EffectHandling.Controllers;
using RoR2;

namespace RiskOfChaos.Patches.Effects.World
{
    static class PauseRunTimerHooks
    {
        [SystemInitializer]
        static void Init()
        {
            On.RoR2.Run.SetRunStopwatchPaused += Run_SetRunStopwatchPaused;
        }

        static void Run_SetRunStopwatchPaused(On.RoR2.Run.orig_SetRunStopwatchPaused orig, Run self, bool isPaused)
        {
            orig(self, isPaused || (ChaosEffectTracker.Instance && ChaosEffectTracker.Instance.IsTimedEffectActive(PauseRunTimer.EffectInfo)));
        }
    }
}
