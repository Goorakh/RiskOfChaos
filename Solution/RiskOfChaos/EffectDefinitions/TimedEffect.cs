using System;

namespace RiskOfChaos.EffectDefinitions
{
    [Obsolete]
    public abstract class TimedEffect : BaseEffect
    {
        [Obsolete]
        public float TimeElapsed
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
