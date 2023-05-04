using System;
using System.Collections.Generic;
using System.Text;

namespace RiskOfChaos.EffectHandling
{
    public readonly struct EffectCanActivateContext
    {
        public static readonly EffectCanActivateContext Now = new EffectCanActivateContext(0f);

        public readonly float Delay;

        public readonly bool IsNow => Delay <= 0f;

        public EffectCanActivateContext(float delay)
        {
            Delay = delay;
        }
    }
}
