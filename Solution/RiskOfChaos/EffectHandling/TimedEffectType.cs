namespace RiskOfChaos.EffectHandling
{
    public enum TimedEffectType
    {
        UntilStageEnd,
        FixedDuration,
        Permanent,
        AlwaysActive
    }

    public enum ConfigTimedEffectType
    {
        UntilStageEnd = TimedEffectType.UntilStageEnd,
        FixedDuration = TimedEffectType.FixedDuration,
        Permanent = TimedEffectType.Permanent
    }
}
