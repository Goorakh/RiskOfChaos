using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RiskOfChaos.Trackers
{
    public class OptionPickupTracker : MonoBehaviour
    {
        [SystemInitializer]
        static void Init()
        {
            static void addTracker(GameObject prefab)
            {
                OptionPickupTracker optionPickupTracker = prefab.AddComponent<OptionPickupTracker>();
                optionPickupTracker._pickupPickerController = prefab.GetComponent<PickupPickerController>();
            }

            Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/OptionPickup/OptionPickup.prefab").OnSuccess(addTracker);
            Addressables.LoadAssetAsync<GameObject>("RoR2/DLC2/FragmentPotentialPickup.prefab").OnSuccess(addTracker);
        }

        public static event Action<OptionPickupTracker> OnStartGlobal;

        PickupPickerController _pickupPickerController;
        public PickupPickerController PickupPickerController => _pickupPickerController;

        void Awake()
        {
            if (!_pickupPickerController)
            {
                _pickupPickerController = GetComponent<PickupPickerController>();
            }
        }

        void OnEnable()
        {
            InstanceTracker.Add(this);
        }

        void Start()
        {
            OnStartGlobal?.Invoke(this);
        }

        void OnDisable()
        {
            InstanceTracker.Remove(this);
        }
    }
}
