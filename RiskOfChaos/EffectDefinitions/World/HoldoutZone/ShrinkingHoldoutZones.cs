using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.World.HoldoutZone
{
    [ChaosTimedEffect("shrinking_holdout_zones", TimedEffectType.UntilStageEnd, AllowDuplicates = false)]
    public sealed class ShrinkingHoldoutZones : GenericHoldoutZoneModifierEffect
    {
        static readonly AnimationCurve _radiusInterpolateCurve = new AnimationCurve(new Keyframe[]
        {
            new Keyframe(0f, 0f, 1f, 1f),
            new Keyframe(0.5f, 0.75f, 1f, 1f),
            new Keyframe(1f, 1f)
        });

        protected override void modifyRadius(HoldoutZoneController controller, ref float radius)
        {
            base.modifyRadius(controller, ref radius);

            radius *= Mathf.Lerp(1f, 1f / 4f, _radiusInterpolateCurve.Evaluate(controller.charge));
        }
    }
}
