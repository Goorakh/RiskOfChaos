namespace RiskOfChaos.ModificationController.TimeScale
{
    public interface ITimeScaleModificationProvider
    {
        bool TryGetTimeScaleModification(out TimeScaleModificationInfo modificationInfo);
    }
}
