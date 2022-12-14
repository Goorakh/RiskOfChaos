using System;
using System.Collections.Generic;
using System.Text;

namespace RiskOfChaos.EffectDefinitions
{
    public abstract class BaseEffect
    {
        public Xoroshiro128Plus RNG;

        public virtual void OnStart()
        {
        }
    }
}
