using RiskOfChaos.Patches;
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
            HoldoutZoneControllerEvents.OnHoldoutZoneControllerAwakeGlobal += onHoldoutZoneControllerAwakeGlobal;
        }

        static void onHoldoutZoneControllerAwakeGlobal(HoldoutZoneController holdoutZoneController)
        {
            HoldoutZoneTracker holdoutZoneTracker = holdoutZoneController.gameObject.AddComponent<HoldoutZoneTracker>();
            holdoutZoneTracker.HoldoutZoneController = holdoutZoneController;
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
