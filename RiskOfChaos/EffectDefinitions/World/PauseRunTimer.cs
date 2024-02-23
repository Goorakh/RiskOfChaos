using MonoMod.Cil;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System.Linq;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosTimedEffect("pause_run_timer", 60f, AllowDuplicates = false)]
    public sealed class PauseRunTimer : TimedEffect
    {
        static bool _appliedPatches = false;
        static void tryApplyPatches()
        {
            if (_appliedPatches)
                return;

            IL.RoR2.Run.FixedUpdate += il =>
            {
                ILCursor c = new ILCursor(il);

                if (c.TryGotoNext(MoveType.Before, x => x.MatchCallOrCallvirt<Run>(nameof(Run.SetRunStopwatchPaused))))
                {
                    c.EmitDelegate((bool isPaused) =>
                    {
                        return isPaused || (TimedChaosEffectHandler.Instance && TimedChaosEffectHandler.Instance.GetActiveEffectInstancesOfType<PauseRunTimer>().Any());
                    });
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
