using RiskOfChaos.Networking.Components;
using RiskOfChaos.Utilities.Interpolation;
using RoR2;
using UnityEngine.Networking;

namespace RiskOfChaos.ModifierController.HoldoutZone
{
    [ValueModificationManager]
    public sealed class HoldoutZoneModificationManager : ValueModificationManager<HoldoutZoneModificationInfo>
    {
        static HoldoutZoneModificationManager _instance;
        public static HoldoutZoneModificationManager Instance => _instance;

        protected override void OnEnable()
        {
            base.OnEnable();
            SingletonHelper.Assign(ref _instance, this);

            HoldoutZoneModifier.OnHoldoutZoneEnabled += modifyHoldoutZone;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            SingletonHelper.Unassign(ref _instance, this);

            HoldoutZoneModifier.OnHoldoutZoneEnabled -= modifyHoldoutZone;
        }

        public override HoldoutZoneModificationInfo InterpolateValue(in HoldoutZoneModificationInfo a, in HoldoutZoneModificationInfo b, float t)
        {
            return HoldoutZoneModificationInfo.Interpolate(a, b, t, ValueInterpolationFunctionType.Linear);
        }

        public override void UpdateValueModifications()
        {
            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return;
            }

            foreach (HoldoutZoneModifier zoneModifier in InstanceTracker.GetInstancesList<HoldoutZoneModifier>())
            {
                modifyHoldoutZone(zoneModifier);
            }
        }

        void modifyHoldoutZone(HoldoutZoneModifier zoneModifier)
        {
            if (!NetworkServer.active)
            {
                Log.Warning("Called on client");
                return;
            }

            HoldoutZoneModificationInfo modificationInfo = GetModifiedValue(new HoldoutZoneModificationInfo(zoneModifier.HoldoutZoneController));

            zoneModifier.RadiusMultiplier = modificationInfo.RadiusMultiplier;
            zoneModifier.ChargeRateMultiplier = modificationInfo.ChargeRateMultiplier;
            zoneModifier.OverrideColor = modificationInfo.ColorOverride;
        }
    }
}
