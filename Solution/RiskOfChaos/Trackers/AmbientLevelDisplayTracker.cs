using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.UI;
using UnityEngine;

namespace RiskOfChaos.Trackers
{
    public class AmbientLevelDisplayTracker : MonoBehaviour
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

            foreach (AmbientLevelDisplay ambientLevelDisplay in hud.gameModeUiRoot.GetComponentsInChildren<AmbientLevelDisplay>(true))
            {
                AmbientLevelDisplayTracker tracker = ambientLevelDisplay.gameObject.EnsureComponent<AmbientLevelDisplayTracker>();
                tracker.AmbientLevelDisplay = ambientLevelDisplay;
            }
        }

        public AmbientLevelDisplay AmbientLevelDisplay { get; private set; }

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
            if (AmbientLevelDisplay)
            {
                // Force refresh next update
                AmbientLevelDisplay.lastLevel = -1;
            }
        }
    }
}
