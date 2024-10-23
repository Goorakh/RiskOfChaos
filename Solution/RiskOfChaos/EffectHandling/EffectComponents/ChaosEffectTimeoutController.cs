using RiskOfChaos.Content;
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

        void FixedUpdate()
        {
            if (Time.deltaTime == 0f || _effectComponent.EffectDestructionHandledByComponent || _effectComponent.IsRetired)
                return;

            _age += Time.fixedUnscaledDeltaTime;
            if (_age > TIMEOUT_DURATION)
            {
                _effectComponent.RetireEffect();
            }
        }
    }
}
