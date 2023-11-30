namespace RiskOfChaos.ModifierController.AttackDelay
{
    public record struct AttackDelayModificationInfo(float TotalDelay)
    {
        public static AttackDelayModificationInfo Interpolate(in AttackDelayModificationInfo a, in AttackDelayModificationInfo b, float t, ValueInterpolationFunctionType interpolationType)
        {
            return new AttackDelayModificationInfo(interpolationType.Interpolate(a.TotalDelay, b.TotalDelay, t));
        }
    }
}
