using RiskOfChaos.EffectHandling;

namespace RiskOfChaos.EffectDefinitions
{
    public abstract class TimedEffect : BaseEffect
    {
        public abstract TimedEffectType TimedType { get; }

        public abstract void OnEnd();
    }
}
