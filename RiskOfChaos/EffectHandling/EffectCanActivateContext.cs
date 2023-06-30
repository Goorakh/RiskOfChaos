namespace RiskOfChaos.EffectHandling
{
    public record struct EffectCanActivateContext(float Delay)
    {
        public static readonly EffectCanActivateContext Now = new EffectCanActivateContext(0f);

        public readonly bool IsNow => Delay <= 0f;
    }
}
