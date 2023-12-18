using RoR2;
using RoR2.UI;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfChaos.Trackers
{
    public class RunTimerUITracker : MonoBehaviour
    {
        static readonly HashSet<RunTimerUIController> _trackedRunTimers = new HashSet<RunTimerUIController>();

        [SystemInitializer]
        static void Init()
        {
            On.RoR2.UI.RunTimerUIController.Update += (orig, self) =>
            {
                orig(self);

                if (_trackedRunTimers.Add(self))
                {
                    RunTimerUITracker tracker = self.gameObject.AddComponent<RunTimerUITracker>();
                    tracker.TimerController = self;
                }
            };

            Run.onRunDestroyGlobal += _ =>
            {
                _trackedRunTimers.Clear();
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

        void OnDestroy()
        {
            _trackedRunTimers.Remove(TimerController);
        }

        public static bool IsAnyTimerVisibleForHUD(HUD hud)
        {
            return hud && InstanceTracker.GetInstancesList<RunTimerUITracker>().Exists(t => t.OwnerHUD == hud);
        }
    }
}
