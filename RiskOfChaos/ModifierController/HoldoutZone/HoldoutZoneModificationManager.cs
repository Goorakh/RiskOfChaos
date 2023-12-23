using RiskOfChaos.Components;
using RiskOfChaos.Utilities.Interpolation;
using RoR2;

namespace RiskOfChaos.ModifierController.HoldoutZone
{
    public sealed class HoldoutZoneModificationManager : ValueModificationManager<HoldoutZoneModificationInfo>
    {
        static HoldoutZoneModificationManager _instance;
        public static HoldoutZoneModificationManager Instance => _instance;

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

        public override HoldoutZoneModificationInfo InterpolateValue(in HoldoutZoneModificationInfo a, in HoldoutZoneModificationInfo b, float t)
        {
            return HoldoutZoneModificationInfo.Interpolate(a, b, t, ValueInterpolationFunctionType.Linear);
        }

        public override void UpdateValueModifications()
        {
            foreach (HoldoutZoneModifier zoneModifier in InstanceTracker.GetInstancesList<HoldoutZoneModifier>())
            {
                HoldoutZoneModificationInfo modificationInfo = GetModifiedValue(new HoldoutZoneModificationInfo(zoneModifier.HoldoutZoneController));

                zoneModifier.RadiusMultiplier = modificationInfo.RadiusMultiplier;
                zoneModifier.ChargeRateMultiplier = modificationInfo.ChargeRateMultiplier;
                zoneModifier.OverrideColor = modificationInfo.ColorOverride;
            }
        }
    }
}
