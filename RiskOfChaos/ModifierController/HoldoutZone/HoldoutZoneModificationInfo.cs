using RiskOfChaos.Utilities.Interpolation;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.ModifierController.HoldoutZone
{
    public struct HoldoutZoneModificationInfo
    {
        public readonly HoldoutZoneController ZoneController;

        public float RadiusMultiplier;
        public float ChargeRateMultiplier;
        public Color? ColorOverride;

        public HoldoutZoneModificationInfo(HoldoutZoneController zoneController)
        {
            ZoneController = zoneController;

            RadiusMultiplier = 1f;
            ChargeRateMultiplier = 1f;
            ColorOverride = null;
        }

        public static HoldoutZoneModificationInfo Interpolate(in HoldoutZoneModificationInfo a, in HoldoutZoneModificationInfo b, float t, ValueInterpolationFunctionType interpolationType)
        {
            if (a.ZoneController != b.ZoneController)
            {
                Log.Warning("Attempting to interpolate modification infos with differing zone controllers");
            }

            Color? colorOverride;
            if (a.ColorOverride.HasValue && b.ColorOverride.HasValue)
            {
                colorOverride = interpolationType.Interpolate(a.ColorOverride.Value, b.ColorOverride.Value, t);
            }
            else
            {
                colorOverride = b.ColorOverride;
            }

            return new HoldoutZoneModificationInfo(b.ZoneController)
            {
                RadiusMultiplier = interpolationType.Interpolate(a.RadiusMultiplier, b.RadiusMultiplier, t),
                ChargeRateMultiplier = interpolationType.Interpolate(a.ChargeRateMultiplier, b.ChargeRateMultiplier, t),
                ColorOverride = colorOverride
            };
        }
    }
}
