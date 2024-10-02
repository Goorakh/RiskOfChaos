using MonoMod.Cil;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RoR2;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosTimedEffect("pause_run_timer", 60f, AllowDuplicates = false)]
    public sealed class PauseRunTimer : TimedEffect
    {
        [InitEffectInfo]
        public static new readonly TimedEffectInfo EffectInfo;

        static bool _appliedPatches = false;
        static void tryApplyPatches()
        {
            if (_appliedPatches)
                return;

            IL.RoR2.Run.FixedUpdate += il =>
            {
                ILCursor c = new ILCursor(il);

                if (c.TryGotoNext(MoveType.Before,
                                  x => x.MatchCallOrCallvirt<Run>(nameof(Run.SetRunStopwatchPaused))))
                {
                    c.EmitDelegate(modifyIsPaused);
                    bool modifyIsPaused(bool isPaused)
                    {
                        return isPaused || (TimedChaosEffectHandler.Instance && TimedChaosEffectHandler.Instance.IsTimedEffectActive(EffectInfo));
                    }
                }
                else
                {
                    Log.Error("Failed to find paused override patch location");
                }
            };

            _appliedPatches = true;
        }

        public override void OnStart()
        {
            tryApplyPatches();

            ChaosEffectActivationSignaler.CanDispatchEffectsOverride += ChaosEffectActivationSignaler_CanDispatchEffectsOverride;
        }

        public override void OnEnd()
        {
            ChaosEffectActivationSignaler.CanDispatchEffectsOverride -= ChaosEffectActivationSignaler_CanDispatchEffectsOverride;
        }

        static bool ChaosEffectActivationSignaler_CanDispatchEffectsOverride()
        {
            return false;
        }
    }
}
