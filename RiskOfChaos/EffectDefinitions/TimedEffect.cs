using System;
using System.Collections.Generic;
using System.Text;

namespace RiskOfChaos.EffectDefinitions
{
    public abstract class TimedEffect : BaseEffect
    {
        public abstract void OnEnd();
    }
}
