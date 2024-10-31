using RiskOfChaos.Utilities;

namespace RiskOfChaos.EffectHandling
{
    public interface IRunTimer
    {
        void SkipAllScheduledActivations();

        void RewindScheduledActivations(float numSeconds);

        int GetNumScheduledActivations();

        void SkipActivations(int numActivationsToSkip);

        RunTimeStamp GetNextActivationTime();
    }
}
