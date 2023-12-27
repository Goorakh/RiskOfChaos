namespace RiskOfChaos.EffectHandling
{
    public record struct EffectCanActivateContext(float Delay, bool IsShortcut)
    {
        public static readonly EffectCanActivateContext Now = new EffectCanActivateContext(0f);

        public static readonly EffectCanActivateContext Now_Shortcut = new EffectCanActivateContext(0f, true);

        public EffectCanActivateContext(float Delay) : this(Delay, false)
        {
        }

        public readonly bool IsNow => Delay <= 0f;
    }
}
