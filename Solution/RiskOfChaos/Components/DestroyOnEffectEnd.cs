using RiskOfChaos.EffectHandling.EffectComponents;
using UnityEngine;

namespace RiskOfChaos.Components
{
    public sealed class DestroyOnEffectEnd : MonoBehaviour
    {
        public ChaosEffectComponent EffectComponent;

        void Start()
        {
            if (EffectComponent)
            {
                EffectComponent.OnEffectEnd += onEffectEnd;
            }
        }

        void OnDestroy()
        {
            if (EffectComponent)
            {
                EffectComponent.OnEffectEnd -= onEffectEnd;
            }
        }

        void onEffectEnd(ChaosEffectComponent effectComponent)
        {
            Destroy(gameObject);
        }
    }
}
