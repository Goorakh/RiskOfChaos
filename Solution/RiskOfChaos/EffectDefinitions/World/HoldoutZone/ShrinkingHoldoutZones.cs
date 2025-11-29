using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectComponents;
using RiskOfChaos.Trackers;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.World.HoldoutZone
{
    [ChaosTimedEffect("shrinking_holdout_zones", TimedEffectType.UntilStageEnd, AllowDuplicates = false)]
    public sealed class ShrinkingHoldoutZones : MonoBehaviour
    {
        ChaosEffectComponent _effectComponent;

        void Awake()
        {
            _effectComponent = GetComponent<ChaosEffectComponent>();
        }

        void Start()
        {
            foreach (HoldoutZoneTracker holdoutZoneTracker in InstanceTracker.GetInstancesList<HoldoutZoneTracker>())
            {
                registerHoldoutZone(holdoutZoneTracker);
            }

            HoldoutZoneTracker.OnHoldoutZoneStartGlobal += registerHoldoutZone;
        }

        void OnDestroy()
        {
            HoldoutZoneTracker.OnHoldoutZoneStartGlobal -= registerHoldoutZone;
        }

        void registerHoldoutZone(HoldoutZoneTracker holdoutZoneTracker)
        {
            HoldoutZoneController holdoutZoneController = holdoutZoneTracker.HoldoutZoneController;
            if (!holdoutZoneController || holdoutZoneController.GetComponent<ShrinkingHoldoutZoneController>())
                return;

            ShrinkingHoldoutZoneController shrinkController = holdoutZoneController.gameObject.AddComponent<ShrinkingHoldoutZoneController>();
            shrinkController.OwnerEffectComponent = _effectComponent;
        }

        sealed class ShrinkingHoldoutZoneController : MonoBehaviour
        {
            static readonly AnimationCurve _radiusMultiplierCurve = new AnimationCurve([
                new Keyframe(0f, 0f, 1f, 1f),
                new Keyframe(0.5f, 0.75f, 1f, 1f),
                new Keyframe(1f, 1f)
            ]);

            HoldoutZoneController _holdoutZone;

            public ChaosEffectComponent OwnerEffectComponent
            {
                get => field;
                set
                {
                    if (field == value)
                        return;

                    if (field)
                    {
                        field.OnEffectEnd -= onOwnerEffectEnd;
                    }

                    field = value;

                    if (field)
                    {
                        field.OnEffectEnd += onOwnerEffectEnd;
                    }
                }
            }

            void Awake()
            {
                _holdoutZone = GetComponent<HoldoutZoneController>();
            }

            void OnEnable()
            {
                _holdoutZone.calcRadius += calcRadius;
            }

            void OnDisable()
            {
                _holdoutZone.calcRadius -= calcRadius;
            }

            void onOwnerEffectEnd(ChaosEffectComponent effectComponent)
            {
                Destroy(this);
            }

            void calcRadius(ref float radius)
            {
                radius *= Mathf.Lerp(1f, 1f / 4f, _radiusMultiplierCurve.Evaluate(_holdoutZone.charge));
            }
        }
    }
}
