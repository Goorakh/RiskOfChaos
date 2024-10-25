using RiskOfChaos.EffectHandling;
using System;

namespace RiskOfChaos.EffectDefinitions
{
    [Obsolete]
    public abstract class TimedEffect : BaseEffect
    {
        [Obsolete]
        public TimedEffectType TimedType { get; internal set; }

        [Obsolete]
        public float TimeElapsed
        {
            get
            {
                return 0;
            }
        }

        [Obsolete]
        public float TimeRemaining
        {
            get
            {
                return 0;
            }
        }

        [Obsolete]
        public abstract void OnEnd();
    }
}
