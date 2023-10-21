using RoR2;
using RoR2.UI;
using System.Linq;
using UnityEngine;

namespace RiskOfChaos.Trackers
{
    public class NotificationUIControllerTracker : MonoBehaviour
    {
        [SystemInitializer]
        static void Init()
        {
            On.RoR2.UI.NotificationUIController.OnEnable += (orig, self) =>
            {
                orig(self);

                NotificationUIControllerTracker tracker = self.gameObject.AddComponent<NotificationUIControllerTracker>();
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
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
            return InstanceTracker.GetInstancesList<NotificationUIControllerTracker>()
                                  .Select(t => t.NotificationUIController)
                                  .FirstOrDefault(c => c.hud == hud);
#pragma warning restore Publicizer001 // Accessing a member that was not originally public
        }
    }
}
