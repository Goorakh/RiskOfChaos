using HG;
using RoR2;
using RoR2.ContentManagement;
using System;
using UnityEngine;

namespace RiskOfChaos.Trackers
{
    public sealed class PickupDistributorBehaviorTracker : MonoBehaviour
    {
        [SystemInitializer]
        static void Init()
        {
            foreach (GameObject prefab in ContentManager.networkedObjectPrefabs)
            {
                foreach (PickupDistributorBehavior pickupDistributorBehavior in prefab.GetComponentsInChildren<PickupDistributorBehavior>(true))
                {
                    PickupDistributorBehaviorTracker tracker = pickupDistributorBehavior.gameObject.EnsureComponent<PickupDistributorBehaviorTracker>();
                    tracker._pickupDistributorBehavior = pickupDistributorBehavior;
                }
            }
        }

        public static event Action<PickupDistributorBehavior> OnPickupDistributorBehaviorStartGlobal;

        [SerializeField]
        PickupDistributorBehavior _pickupDistributorBehavior;
        public PickupDistributorBehavior PickupDistributorBehavior => _pickupDistributorBehavior;

        void Start()
        {
            InstanceTracker.Add(this);
            OnPickupDistributorBehaviorStartGlobal?.Invoke(PickupDistributorBehavior);
        }

        void OnDestroy()
        {
            InstanceTracker.Remove(this);
        }
    }
}
