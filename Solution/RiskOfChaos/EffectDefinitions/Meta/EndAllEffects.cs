using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Utilities.Extensions;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Meta
{
    [ChaosEffect("end_all_effects", DefaultSelectionWeight = 0.7f)]
    public class EndAllEffects : MonoBehaviour
    {
        [EffectCanActivate]
        static bool CanActivate(in EffectCanActivateContext context)
        {
            if (ChaosEffectTracker.Instance)
            {
                foreach (ChaosEffectComponent effectComponent in ChaosEffectTracker.Instance.AllActiveTimedEffects)
                {
                    if (effectComponent.ChaosEffectInfo is TimedEffectInfo timedEffectInfo)
                    {
                        if (ChaosEffectTracker.Instance.IsAnyInstanceOfTimedEffectRelevantForContext(timedEffectInfo, context))
                            return true;
                    }
                }
            }

            return false;
        }

        ChaosEffectComponent _effectComponent;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        void Start()
        {
            if (!NetworkServer.active)
                return;

            List<ChaosEffectComponent> effectsToEnd = [];
            if (ChaosEffectTracker.Instance)
            {
                effectsToEnd.EnsureCapacity(ChaosEffectTracker.Instance.AllActiveTimedEffects.Count);
                foreach (ChaosEffectComponent effectComponent in ChaosEffectTracker.Instance.AllActiveTimedEffects)
                {
                    if (effectComponent == _effectComponent)
                        continue;
                    
                    if (effectComponent.TryGetComponent(out ChaosEffectDurationComponent durationComponent))
                    {
                        if (durationComponent.TimedType == TimedEffectType.AlwaysActive)
                            continue;
                    }

                    effectsToEnd.Add(effectComponent);
                }
            }

            foreach (ChaosEffectComponent effectComponent in effectsToEnd)
            {
                effectComponent.RetireEffect();
            }
        }
    }
}
