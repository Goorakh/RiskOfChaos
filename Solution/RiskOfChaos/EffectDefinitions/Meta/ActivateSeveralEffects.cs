using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Meta
{
    [ChaosEffect("activate_several_effects", DefaultSelectionWeight = 0.7f)]
    public sealed class ActivateSeveralEffects : NetworkBehaviour
    {
        [InitEffectInfo]
        static readonly ChaosEffectInfo _effectInfo;

        [EffectConfig]
        static readonly ConfigHolder<int> _numEffectsToActivate =
            ConfigFactory<int>.CreateConfig("Effect Count", 2)
                              .Description("How many effects should be activated by this effect")
                              .AcceptableValues(new AcceptableValueMin<int>(1))
                              .OptionConfig(new IntFieldConfig { Min = 1 })
                              .Build();

        [EffectConfig]
        static readonly ConfigHolder<bool> _allowDuplicateEffects =
            ConfigFactory<bool>.CreateConfig("Allow Duplicate Effects", true)
                               .Description("If the effect can select duplicate effects to activate")
                               .OptionConfig(new CheckBoxConfig())
                               .Build();

        [PrefabInitializer]
        static void InitPrefab(GameObject prefab)
        {
            if (prefab.TryGetComponent(out ChaosEffectComponent effectComponent))
            {
                effectComponent.EffectDestructionHandledByComponent = true;
            }
        }

        ChaosEffectComponent _effectComponent;

        Xoroshiro128Plus _rng;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            _rng = new Xoroshiro128Plus(_effectComponent.Rng.nextUlong);
        }

        IEnumerator Start()
        {
            if (!NetworkServer.active)
                yield break;

            const float INITIAL_DELAY = 0.2f;
            const float EFFECT_DELAY = 0.3f;

            float totalDelay = 0f;

            totalDelay += INITIAL_DELAY;
            yield return new WaitForSeconds(INITIAL_DELAY);

            RunTimeStamp effectStartTime = _effectComponent.TimeStarted;

            bool allowDuplicateEffects = _allowDuplicateEffects.Value;

            HashSet<ChaosEffectInfo> excludeEffects = [_effectInfo];

            if (!allowDuplicateEffects)
            {
                excludeEffects.EnsureCapacity(excludeEffects.Count + _numEffectsToActivate.Value);
            }

            for (int i = 0; i < _numEffectsToActivate.Value; i++)
            {
                totalDelay += EFFECT_DELAY;
                yield return new WaitForSeconds(EFFECT_DELAY);

                ChaosEffectInfo effect = ChaosEffectActivationSignaler.PickEffect(_rng.Branch(), excludeEffects, out ChaosEffectDispatchArgs dispatchArgs);
                dispatchArgs.OverrideStartTime = effectStartTime + Mathf.FloorToInt(totalDelay);
                dispatchArgs.RNGSeed = _rng.nextUlong;
                ChaosEffectDispatcher.Instance.DispatchEffectServer(effect, dispatchArgs);

                if (!allowDuplicateEffects)
                {
                    excludeEffects.Add(effect);
                }
            }

            _effectComponent.RetireEffect();
        }
    }
}
