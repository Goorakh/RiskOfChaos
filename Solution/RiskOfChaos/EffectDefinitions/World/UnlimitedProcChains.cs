using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RoR2;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosTimedEffect("unlimited_proc_chains", 60f, AllowDuplicates = false, IsNetworked = true)]
    public sealed class UnlimitedProcChains : TimedEffect
    {
        [InitEffectInfo]
        static readonly TimedEffectInfo _effectInfo;

        static bool _appliedPatches;
        static void tryApplyPatches()
        {
            if (_appliedPatches)
                return;

            _appliedPatches = true;

            On.RoR2.ProcChainMask.HasProc += ProcChainMask_HasProc;
        }

        static bool ProcChainMask_HasProc(On.RoR2.ProcChainMask.orig_HasProc orig, ref ProcChainMask self, ProcType procType)
        {
            return orig(ref self, procType) && !(TimedChaosEffectHandler.Instance && TimedChaosEffectHandler.Instance.IsTimedEffectActive(_effectInfo));
        }

        public override void OnStart()
        {
            tryApplyPatches();
        }

        public override void OnEnd()
        {
        }
    }
}
