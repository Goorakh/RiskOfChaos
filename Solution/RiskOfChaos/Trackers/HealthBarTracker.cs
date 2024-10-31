using RoR2;
using RoR2.UI;
using UnityEngine;

namespace RiskOfChaos.Trackers
{
    public sealed class HealthBarTracker : MonoBehaviour
    {
        [SystemInitializer]
        static void Init()
        {
            On.RoR2.UI.HealthBar.Awake += HealthBar_Awake;
        }

        static void HealthBar_Awake(On.RoR2.UI.HealthBar.orig_Awake orig, HealthBar self)
        {
            orig(self);

            HealthBarTracker tracker = self.gameObject.AddComponent<HealthBarTracker>();
            tracker.HealthBar = self;
        }

        public delegate void HealthBarEventDelegate(HealthBarTracker healthBarTracker);
        public static event HealthBarEventDelegate OnHealthBarAwakeGlobal;

        bool _isTracked;

        HealthBar _healthBar;
        public HealthBar HealthBar
        {
            get
            {
                return _healthBar;
            }
            private set
            {
                _healthBar = value;
                setIsTracked(HealthBar);
            }
        }

        void Awake()
        {
            setIsTracked(HealthBar);
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
                OnHealthBarAwakeGlobal?.Invoke(this);
            }
            else
            {
                InstanceTracker.Remove(this);
            }
        }
    }
}
