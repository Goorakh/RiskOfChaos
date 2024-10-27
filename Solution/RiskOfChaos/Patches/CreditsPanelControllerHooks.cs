using RoR2;
using RoR2.UI;

namespace RiskOfChaos.Patches
{
    static class CreditsPanelControllerHooks
    {
        public delegate void CreditsPanelControllerEventDelegate(CreditsPanelController creditsPanelController);
        public static event CreditsPanelControllerEventDelegate OnCreditsPanelControllerEnableGlobal;

        [SystemInitializer]
        static void Init()
        {
            On.RoR2.UI.CreditsPanelController.OnEnable += CreditsPanelController_OnEnable;
        }

        static void CreditsPanelController_OnEnable(On.RoR2.UI.CreditsPanelController.orig_OnEnable orig, CreditsPanelController self)
        {
            orig(self);
            OnCreditsPanelControllerEnableGlobal?.Invoke(self);
        }
    }
}
