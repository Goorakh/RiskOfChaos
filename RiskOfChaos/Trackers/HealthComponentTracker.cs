using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
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

        void Awake()
        {
            _healthComponent = GetComponent<HealthComponent>();
        }

        void OnEnable()
        {
            InstanceTracker.Add(_healthComponent);
        }

        void OnDisable()
        {
            InstanceTracker.Remove(_healthComponent);
        }
    }
}
