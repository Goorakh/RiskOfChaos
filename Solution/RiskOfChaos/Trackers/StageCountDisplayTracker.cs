using HG;
using RoR2;
using RoR2.UI;
using UnityEngine;

namespace RiskOfChaos.Trackers
{
    public sealed class StageCountDisplayTracker : MonoBehaviour
    {
        [SystemInitializer]
        static void Init()
        {
            HudTracker.OnHudStartGlobal += onHudStartGlobal;
        }

        static void onHudStartGlobal(HUD hud)
        {
            if (!hud.gameModeUiRoot)
                return;

            foreach (StageCountDisplay stageCountDisplay in hud.gameModeUiRoot.GetComponentsInChildren<StageCountDisplay>(true))
            {
                StageCountDisplayTracker tracker = stageCountDisplay.gameObject.EnsureComponent<StageCountDisplayTracker>();
                tracker.StageCountDisplay = stageCountDisplay;
            }
        }

        public StageCountDisplay StageCountDisplay { get; private set; }

        void OnEnable()
        {
            Language.onCurrentLanguageChanged += onCurrentLanguageChanged;
        }

        void OnDisable()
        {
            Language.onCurrentLanguageChanged -= onCurrentLanguageChanged;
        }

        void onCurrentLanguageChanged()
        {
            if (StageCountDisplay)
            {
                // Force refresh next update
                StageCountDisplay.lastStage = -1;
            }
        }
    }
}
