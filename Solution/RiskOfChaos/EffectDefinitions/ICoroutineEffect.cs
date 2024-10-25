using System;
using System.Collections;

namespace RiskOfChaos.EffectDefinitions
{
    [Obsolete]
    public interface ICoroutineEffect
    {
        IEnumerator OnStartCoroutine();

        void OnForceStopped();
    }
}
