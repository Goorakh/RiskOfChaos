using RoR2;
using RoR2.UI;
using UnityEngine;

namespace RiskOfChaos.Trackers
{
    public sealed class HUDBossHealthBarControllerTracker : MonoBehaviour
    {
        [SystemInitializer]
        static void Init()
        {
            On.RoR2.UI.HUDBossHealthBarController.OnEnable += HUDBossHealthBarController_OnEnable;
        }

        static void HUDBossHealthBarController_OnEnable(On.RoR2.UI.HUDBossHealthBarController.orig_OnEnable orig, HUDBossHealthBarController self)
        {
            orig(self);

            if (!self.TryGetComponent(out HUDBossHealthBarControllerTracker tracker))
            {
                tracker = self.gameObject.AddComponent<HUDBossHealthBarControllerTracker>();
            }

            tracker.HUDBossHealthBarController = self;
        }

        bool _isTracked;

        HUDBossHealthBarController _hudBossHealthBarController;
        public HUDBossHealthBarController HUDBossHealthBarController
        {
            get
            {
                return _hudBossHealthBarController;
            }
            private set
            {
                if (_hudBossHealthBarController == value)
                    return;

                _hudBossHealthBarController = value;

                HealthBarRoot = null;
                if (HUDBossHealthBarController && HUDBossHealthBarController.container)
                {
                    Transform healthBarRootTransform = HUDBossHealthBarController.container.transform.Find("BossHealthBarContainer");
                    if (healthBarRootTransform)
                    {
                        HealthBarRoot = healthBarRootTransform.gameObject;
                    }
                }

                setIsTracked(HUDBossHealthBarController);
            }
        }

        public GameObject HealthBarRoot { get; private set; }

        void Awake()
        {
            setIsTracked(HUDBossHealthBarController);
        }

        void OnDestroy()
        {
            setIsTracked(false);
        }

        void setIsTracked(bool isTracked)
        {
            if (isTracked == _isTracked)
                return;

            _isTracked = isTracked;

            if (_isTracked)
            {
                InstanceTracker.Add(this);
            }
            else
            {
                InstanceTracker.Remove(this);
            }
        }
    }
}
