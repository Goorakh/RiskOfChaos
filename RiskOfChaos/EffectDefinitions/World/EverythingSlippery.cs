using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Networking;
using RiskOfChaos.Patches;
using RoR2;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosEffect("everything_slippery", EffectActivationCountHardCap = 1, IsNetworked = true)]
    public sealed class EverythingSlippery : BaseEffect
    {
        public override void OnStart()
        {
            OverrideAllSurfacesSlippery.IsActive = true;

            Run.onRunDestroyGlobal += Run_onRunDestroyGlobal;
            StageCompleteMessage.OnReceive += StageCompleteMessage_OnReceive;
        }

        static void Run_onRunDestroyGlobal(Run _)
        {
            disablePatch();
        }

        static void StageCompleteMessage_OnReceive(Stage _)
        {
            disablePatch();
        }

        static void disablePatch()
        {
            OverrideAllSurfacesSlippery.IsActive = false;

            Run.onRunDestroyGlobal -= Run_onRunDestroyGlobal;
            StageCompleteMessage.OnReceive -= StageCompleteMessage_OnReceive;
        }
    }
}
