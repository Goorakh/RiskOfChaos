using RoR2;
using System;
using UnityEngine;

namespace RiskOfChaos.Trackers
{
    public sealed class HoldoutZoneTracker : MonoBehaviour
    {
        public static event Action<HoldoutZoneTracker> OnHoldoutZoneStartGlobal;

        [SystemInitializer]
        static void Init()
        {
            On.RoR2.HoldoutZoneController.Awake += HoldoutZoneController_Awake;
        }

        static void HoldoutZoneController_Awake(On.RoR2.HoldoutZoneController.orig_Awake orig, HoldoutZoneController self)
        {
            orig(self);

            HoldoutZoneTracker holdoutZoneTracker = self.gameObject.AddComponent<HoldoutZoneTracker>();
            holdoutZoneTracker.HoldoutZoneController = self;
        }

        public HoldoutZoneController HoldoutZoneController { get; private set; }

        void Start()
        {
            InstanceTracker.Add(this);

            OnHoldoutZoneStartGlobal?.Invoke(this);
        }

        void OnDestroy()
        {
            InstanceTracker.Remove(this);
        }
    }
}
