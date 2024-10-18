using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.Trackers;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System.Collections.Generic;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.World.HoldoutZone
{
    [ChaosTimedEffect("shrinking_holdout_zones", TimedEffectType.UntilStageEnd, AllowDuplicates = false)]
    public sealed class ShrinkingHoldoutZones : MonoBehaviour
    {
        readonly List<ShrinkingHoldoutZoneController> _addedShrinkComponents = [];

        void Start()
        {
            List<HoldoutZoneTracker> holdoutZoneTrackers = InstanceTracker.GetInstancesList<HoldoutZoneTracker>();
            _addedShrinkComponents.EnsureCapacity(holdoutZoneTrackers.Count);

            foreach (HoldoutZoneTracker holdoutZoneTracker in holdoutZoneTrackers)
            {
                registerHoldoutZone(holdoutZoneTracker);
            }

            HoldoutZoneTracker.OnHoldoutZoneStartGlobal += registerHoldoutZone;
        }

        void OnDestroy()
        {
            HoldoutZoneTracker.OnHoldoutZoneStartGlobal -= registerHoldoutZone;

            foreach (ShrinkingHoldoutZoneController shrinkComponent in _addedShrinkComponents)
            {
                if (shrinkComponent)
                {
                    Destroy(shrinkComponent);
                }
            }

            _addedShrinkComponents.Clear();
        }

        void registerHoldoutZone(HoldoutZoneTracker holdoutZoneTracker)
        {
            HoldoutZoneController holdoutZoneController = holdoutZoneTracker.HoldoutZoneController;
            if (!holdoutZoneController || holdoutZoneController.GetComponent<ShrinkingHoldoutZoneController>())
                return;

            ShrinkingHoldoutZoneController shrinkComponent = holdoutZoneController.gameObject.AddComponent<ShrinkingHoldoutZoneController>();
            _addedShrinkComponents.Add(shrinkComponent);
        }

        class ShrinkingHoldoutZoneController : MonoBehaviour
        {
            static readonly AnimationCurve _radiusMultiplierCurve = new AnimationCurve([
                new Keyframe(0f, 0f, 1f, 1f),
                new Keyframe(0.5f, 0.75f, 1f, 1f),
                new Keyframe(1f, 1f)
            ]);

            HoldoutZoneController _holdoutZone;

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

            void calcRadius(ref float radius)
            {
                radius *= Mathf.Lerp(1f, 1f / 4f, _radiusMultiplierCurve.Evaluate(_holdoutZone.charge));
            }
        }
    }
}
