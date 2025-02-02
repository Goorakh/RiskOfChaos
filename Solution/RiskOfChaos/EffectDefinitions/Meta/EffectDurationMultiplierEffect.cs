using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.ModificationController;
using RiskOfChaos.ModificationController.Effect;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Meta
{
    public sealed class EffectDurationMultiplierEffect : MonoBehaviour
    {
        [EffectCanActivate]
        static bool CanActivate()
        {
            return RoCContent.NetworkedPrefabs.EffectModificationProvider;
        }

        float _durationMultiplier = 1f;
        public float DurationMultiplier
        {
            get
            {
                return _durationMultiplier;
            }
            set
            {
                if (_durationMultiplier == value)
                    return;

                _durationMultiplier = value;
                refreshDurationMultiplier();
            }
        }

        ChaosEffectComponent _effectComponent;

        ValueModificationController _effectModificationController;
        EffectModificationProvider _effectModificationProvider;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        void Start()
        {
            if (NetworkServer.active)
            {
                _effectModificationController = Instantiate(RoCContent.NetworkedPrefabs.EffectModificationProvider).GetComponent<ValueModificationController>();

                _effectModificationProvider = _effectModificationController.GetComponent<EffectModificationProvider>();
                refreshDurationMultiplier();

                NetworkServer.Spawn(_effectModificationController.gameObject);

                if (ChaosEffectTracker.Instance)
                {
                    foreach (ChaosEffectComponent effectComponent in ChaosEffectTracker.Instance.AllActiveTimedEffects)
                    {
                        if (effectComponent != _effectComponent && effectComponent.DurationComponent)
                        {
                            TimedEffectInfo effectInfo = effectComponent.DurationComponent.TimedEffectInfo;
                            if (effectInfo != null && !effectInfo.IgnoreDurationModifiers)
                            {
                                effectComponent.DurationComponent.Remaining *= DurationMultiplier;
                            }
                        }
                    }
                }
            }
        }

        void OnDestroy()
        {
            if (_effectModificationController)
            {
                _effectModificationController.Retire();
                _effectModificationController = null;
                _effectModificationProvider = null;
            }
        }

        void refreshDurationMultiplier()
        {
            if (_effectModificationProvider)
            {
                _effectModificationProvider.DurationMultiplier = DurationMultiplier;
            }
        }
    }
}
