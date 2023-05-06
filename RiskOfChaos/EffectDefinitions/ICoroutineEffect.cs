using RiskOfChaos.EffectHandling.Controllers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace RiskOfChaos.EffectDefinitions
{
    public interface ICoroutineEffect
    {
        IEnumerator OnStartCoroutine();

        void OnForceStopped();
    }
}
