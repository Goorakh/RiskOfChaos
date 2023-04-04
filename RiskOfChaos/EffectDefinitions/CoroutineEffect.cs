using RiskOfChaos.EffectHandling.Controllers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace RiskOfChaos.EffectDefinitions
{
    public abstract class CoroutineEffect : BaseEffect
    {
        public override void OnStart()
        {
        }

        public abstract IEnumerator OnStartCoroutine();

        public virtual void OnForceStopped()
        {
        }
    }
}
