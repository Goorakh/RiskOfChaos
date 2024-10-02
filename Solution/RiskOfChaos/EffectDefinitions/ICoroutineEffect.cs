using System.Collections;

namespace RiskOfChaos.EffectDefinitions
{
    public interface ICoroutineEffect
    {
        IEnumerator OnStartCoroutine();

        void OnForceStopped();
    }
}
