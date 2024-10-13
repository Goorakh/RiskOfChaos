using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Interpolation;
using System;
using UnityEngine;

namespace RiskOfChaos.OLD_ModifierController.Pickups
{
    [ValueModificationManager]
    public class PickupModificationManager : ValueModificationManager<PickupModificationInfo>
    {
        static PickupModificationManager _instance;
        public static PickupModificationManager Instance => _instance;

        protected override void OnEnable()
        {
            base.OnEnable();

            SingletonHelper.Assign(ref _instance, this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            SingletonHelper.Unassign(ref _instance, this);
        }

        public uint BounceCount { get; private set; }

        public byte SpawnCountMultiplier { get; private set; }

        public override PickupModificationInfo InterpolateValue(in PickupModificationInfo a, in PickupModificationInfo b, float t)
        {
            return PickupModificationInfo.Interpolate(a, b, t, ValueInterpolationFunctionType.Linear);
        }

        public override void UpdateValueModifications()
        {
            PickupModificationInfo modificationInfo = GetModifiedValue(new PickupModificationInfo());
            BounceCount = modificationInfo.BounceCount;
            SpawnCountMultiplier = Math.Max((byte)1, ClampedConversion.UInt8(Mathf.RoundToInt(modificationInfo.SpawnCountMultiplier)));
        }
    }
}
