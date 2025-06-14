using HG;
using RoR2;
using RoR2.UI;
using UnityEngine;

namespace RiskOfChaos.Trackers
{
    public class InfiniteTowerWaveCounterTracker : MonoBehaviour
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

            foreach (InfiniteTowerWaveCounter infiniteTowerWaveCounter in hud.gameModeUiRoot.GetComponentsInChildren<InfiniteTowerWaveCounter>(true))
            {
                InfiniteTowerWaveCounterTracker tracker = infiniteTowerWaveCounter.gameObject.EnsureComponent<InfiniteTowerWaveCounterTracker>();
                tracker.InfiniteTowerWaveCounter = infiniteTowerWaveCounter;
            }
        }

        public InfiniteTowerWaveCounter InfiniteTowerWaveCounter { get; private set; }

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
            if (InfiniteTowerWaveCounter)
            {
                // Refresh wave counter format string which is cached on enable
                if (InfiniteTowerWaveCounter.enabled)
                {
                    InfiniteTowerWaveCounter.enabled = false;
                    InfiniteTowerWaveCounter.enabled = true;
                }

                // Force refresh next update
                InfiniteTowerWaveCounter.oldWaveIndex = -1;
            }
        }
    }
}
