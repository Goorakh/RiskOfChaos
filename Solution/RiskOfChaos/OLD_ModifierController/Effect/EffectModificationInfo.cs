using RiskOfChaos.Utilities.Interpolation;

namespace RiskOfChaos.OLD_ModifierController.Effect
{
    public struct EffectModificationInfo
    {
        public float DurationMultiplier = 1f;

        public EffectModificationInfo()
        {
        }

        public static EffectModificationInfo Interpolate(in EffectModificationInfo a, in EffectModificationInfo b, float t, ValueInterpolationFunctionType interpolationType)
        {
            return new EffectModificationInfo
            {
                DurationMultiplier = interpolationType.Interpolate(a.DurationMultiplier, b.DurationMultiplier, t)
            };
        }
    }
}
