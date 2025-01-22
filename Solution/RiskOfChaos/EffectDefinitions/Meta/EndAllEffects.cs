using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Meta
{
    [ChaosEffect("end_all_effects", DefaultSelectionWeight = 0.7f)]
    public class EndAllEffects : MonoBehaviour
    {
        [EffectConfig]
        static readonly ConfigHolder<bool> _excludePermanentEffects =
            ConfigFactory<bool>.CreateConfig("Exclude Permanent Effects", true)
                               .Description("If permanent effects should be excluded.")
                               .OptionConfig(new CheckBoxConfig())
                               .Build();

        static bool canEndEffect(ChaosEffectComponent effectComponent, in EffectCanActivateContext context)
        {
            if (effectComponent.TryGetComponent(out ChaosEffectDurationComponent durationComponent))
            {
                if (durationComponent.TimedType == TimedEffectType.AlwaysActive)
                    return false;

                if (durationComponent.TimedType == TimedEffectType.Permanent && _excludePermanentEffects.Value)
                    return false;
            }

            return effectComponent.IsRelevantForContext(context);
        }

        [EffectCanActivate]
        static bool CanActivate(in EffectCanActivateContext context)
        {
            if (ChaosEffectTracker.Instance)
            {
                foreach (ChaosEffectComponent effectComponent in ChaosEffectTracker.Instance.AllActiveTimedEffects)
                {
                    if (canEndEffect(effectComponent, context))
                    {
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
                EffectCanActivateContext activateContext = EffectCanActivateContext.Now;

                effectsToEnd.EnsureCapacity(ChaosEffectTracker.Instance.AllActiveTimedEffects.Count);
                foreach (ChaosEffectComponent effectComponent in ChaosEffectTracker.Instance.AllActiveTimedEffects)
                {
                    if (effectComponent != _effectComponent && canEndEffect(effectComponent, activateContext))
                    {
                        effectsToEnd.Add(effectComponent);
                    }
                }
            }

            foreach (ChaosEffectComponent effectComponent in effectsToEnd)
            {
                effectComponent.RetireEffect();
            }
        }
    }
}
