namespace RiskOfChaos.ModifierController.TimeScale
{
    public interface ITimeScaleModificationProvider : IValueModificationProvider<float>
    {
        bool ContributeToPlayerRealtimeTimeScalePatch { get; }
    }
}
