using HG;
using RiskOfChaos.Utilities;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.Trackers
{
    [RequireComponent(typeof(HealthComponent))]
    public sealed class HealthComponentTracker : MonoBehaviour
    {
        [SystemInitializer]
        static void Init()
        {
            On.RoR2.HealthComponent.Awake += static (orig, self) =>
            {
                orig(self);

                self.gameObject.EnsureComponent<HealthComponentTracker>();
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

        public override string ToString()
        {
            const string FORMAT = $"{nameof(HealthComponentTracker)} ({{0}})";

            if (!HealthComponent)
            {
                return base.ToString();
            }
            else if (!HealthComponent.body)
            {
                return string.Format(FORMAT, HealthComponent);
            }
            else
            {
                return string.Format(FORMAT, FormatUtils.GetBestBodyName(HealthComponent.body));
            }
        }
    }
}
