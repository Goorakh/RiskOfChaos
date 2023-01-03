using RiskOfChaos.EffectHandling;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace RiskOfChaos.EffectDefinitions
{
    public abstract class CoroutineEffect : BaseEffect
    {
        public sealed override void OnStart()
        {
            ChaosEffectDispatcher.Instance.StartCoroutine(onStart());
        }

        protected abstract IEnumerator onStart();
    }
}
