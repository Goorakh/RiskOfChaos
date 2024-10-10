using RiskOfChaos.EffectHandling.EffectClassAttributes;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling.EffectComponents
{
    [DisallowMultipleComponent]
    [RequiredComponents(typeof(ChaosEffectComponent))]
    public class ChaosEffectTimeoutController : MonoBehaviour
    {
        const float TIMEOUT_DURATION = 3f;

        ChaosEffectComponent _effectComponent;

        float _age;

        void Awake()
        {
            if (!NetworkServer.active)
            {
                enabled = false;
                return;
            }

            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        void Update()
        {
            if (Time.deltaTime == 0f || _effectComponent.EffectDestructionHandledByComponent)
                return;

            _age += Time.unscaledDeltaTime;
            if (_age > TIMEOUT_DURATION)
            {
                _effectComponent.RetireEffect();
            }
        }
    }
}
