using RoR2;
using RoR2.CameraModes;

namespace RiskOfChaos.Patches
{
    static class CameraModeHooks
    {
        public delegate void BaseUpdatePostfixDelegate(CameraModeBase cameraMode, ref CameraModeBase.CameraModeContext context, ref CameraModeBase.UpdateResult result);
        public static event BaseUpdatePostfixDelegate OnBaseUpdatePostfix;

        [SystemInitializer]
        static void Init()
        {
            On.RoR2.CameraModes.CameraModeBase.Update += CameraModeBase_Update;
        }

        static void CameraModeBase_Update(On.RoR2.CameraModes.CameraModeBase.orig_Update orig, CameraModeBase self, ref CameraModeBase.CameraModeContext context, out CameraModeBase.UpdateResult result)
        {
            orig(self, ref context, out result);

            OnBaseUpdatePostfix?.Invoke(self, ref context, ref result);
        }
    }
}
