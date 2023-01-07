using RoR2;
using UnityEngine;

namespace RiskOfChaos.Trackers
{
    [RequireComponent(typeof(HealthComponent))]
    public class HealthComponentTracker : MonoBehaviour
    {
        [SystemInitializer]
        static void Init()
        {
            On.RoR2.HealthComponent.Awake += static (orig, self) =>
            {
                orig(self);

                if (!self.GetComponent<HealthComponentTracker>())
                {
                    self.gameObject.AddComponent<HealthComponentTracker>();
                }
            };
        }

        HealthComponent _healthComponent;
        public HealthComponent HealthComponent => _healthComponent;

        void Awake()
        {
            _healthComponent = GetComponent<HealthComponent>();
        }

        void OnEnable()
        {
            InstanceTracker.Add(this);
        }

        void OnDisable()
        {
            InstanceTracker.Remove(this);
        }
    }
}
