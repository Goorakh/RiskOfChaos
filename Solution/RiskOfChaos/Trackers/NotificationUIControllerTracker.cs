using HG;
using RoR2;
using RoR2.UI;
using UnityEngine;

namespace RiskOfChaos.Trackers
{
    [DisallowMultipleComponent]
    public class NotificationUIControllerTracker : MonoBehaviour
    {
        [SystemInitializer]
        static void Init()
        {
            On.RoR2.UI.NotificationUIController.OnEnable += (orig, self) =>
            {
                orig(self);

                NotificationUIControllerTracker tracker = self.gameObject.EnsureComponent<NotificationUIControllerTracker>();
                tracker.NotificationUIController = self;
            };
        }

        public NotificationUIController NotificationUIController { get; private set; }

        void OnEnable()
        {
            InstanceTracker.Add(this);
        }

        void OnDisable()
        {
            InstanceTracker.Remove(this);
        }

        public static NotificationUIController GetNotificationUIControllerForHUD(HUD hud)
        {
            foreach (NotificationUIControllerTracker tracker in InstanceTracker.GetInstancesList<NotificationUIControllerTracker>())
            {
                NotificationUIController notificationUIController = tracker.NotificationUIController;
                if (notificationUIController && notificationUIController.hud == hud)
                {
                    return notificationUIController;
                }
            }

            return null;
        }
    }
}
