using RiskOfChaos.Utilities.Extensions;
using RoR2;
using RoR2.UI;
using UnityEngine;

namespace RiskOfChaos.Trackers
{
    [DisallowMultipleComponent]
    public class RunTimerUITracker : MonoBehaviour
    {
        [SystemInitializer]
        static void Init()
        {
            On.RoR2.UI.RunTimerUIController.Start += (orig, self) =>
            {
                orig(self);

                RunTimerUITracker tracker = self.gameObject.EnsureComponent<RunTimerUITracker>();
                tracker.TimerController = self;
            };
        }

        public HUD OwnerHUD { get; private set; }

        public RunTimerUIController TimerController { get; private set; }

        void refreshOwnerHUD()
        {
            OwnerHUD = GetComponentInParent<HUD>();
        }

        void Awake()
        {
            refreshOwnerHUD();
        }

        void OnTransformParentChanged()
        {
            refreshOwnerHUD();
        }

        void OnEnable()
        {
            InstanceTracker.Add(this);
        }

        void OnDisable()
        {
            InstanceTracker.Remove(this);
        }

        public static bool IsAnyTimerVisibleForHUD(HUD hud)
        {
            return hud && InstanceTracker.GetInstancesList<RunTimerUITracker>().Exists(t => t.OwnerHUD == hud);
        }
    }
}
