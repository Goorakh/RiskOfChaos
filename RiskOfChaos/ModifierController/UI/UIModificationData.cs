using RiskOfChaos.Utilities.Interpolation;

namespace RiskOfChaos.ModifierController.UI
{
    public struct UIModificationData
    {
        public float ScaleMultiplier = 1f;

        public UIModificationData()
        {
        }

        public static UIModificationData Interpolate(in UIModificationData a, in UIModificationData b, float t, ValueInterpolationFunctionType interpolationType)
        {
            return new UIModificationData
            {
                ScaleMultiplier = interpolationType.Interpolate(a.ScaleMultiplier, b.ScaleMultiplier, t)
            };
        }
    }
}
