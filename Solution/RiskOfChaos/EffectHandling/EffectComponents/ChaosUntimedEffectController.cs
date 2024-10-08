using RiskOfChaos.EffectHandling.EffectClassAttributes;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling.EffectComponents
{
    [DisallowMultipleComponent]
    [RequiredComponents(typeof(ChaosEffectComponent))]
    public class ChaosUntimedEffectController : MonoBehaviour
    {
        const float DURATION = 3f;

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
            if (Time.deltaTime == 0f)
                return;

            _age += Time.unscaledDeltaTime;
            if (_age > DURATION)
            {
                _effectComponent.RetireEffect();
            }
        }
    }
}
