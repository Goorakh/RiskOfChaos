using HG;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System;
using UnityEngine;

namespace RiskOfChaos.Trackers
{
    public sealed class OptionPickupTracker : MonoBehaviour
    {
        [SystemInitializer]
        static void Init()
        {
            static void addTracker(string prefabAssetGuid)
            {
                AddressableUtil.LoadTempAssetAsync<GameObject>(prefabAssetGuid).OnSuccess(prefab =>
                {
                    OptionPickupTracker optionPickupTracker = prefab.EnsureComponent<OptionPickupTracker>();
                    optionPickupTracker._pickupPickerController = prefab.GetComponent<PickupPickerController>();
                });
            }

            addTracker(AddressableGuids.RoR2_DLC1_OptionPickup_OptionPickup_prefab);
            addTracker(AddressableGuids.RoR2_DLC2_FragmentPotentialPickup_prefab);
        }

        public static event Action<OptionPickupTracker> OnStartGlobal;

        [SerializeField]
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
