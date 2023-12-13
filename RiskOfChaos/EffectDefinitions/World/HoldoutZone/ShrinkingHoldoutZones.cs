using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.ModifierController.HoldoutZone;
using RoR2;
using System;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.World.HoldoutZone
{
    [ChaosTimedEffect("shrinking_holdout_zones", TimedEffectType.UntilStageEnd, AllowDuplicates = false)]
    public sealed class ShrinkingHoldoutZones : TimedEffect, IHoldoutZoneModificationProvider
    {
        static readonly AnimationCurve _radiusInterpolateCurve = new AnimationCurve(new Keyframe[]
        {
            new Keyframe(0f, 0f, 1f, 1f),
            new Keyframe(0.5f, 0.75f, 1f, 1f),
            new Keyframe(1f, 1f)
        });

        [EffectCanActivate]
        static bool CanActivate()
        {
            return HoldoutZoneModificationManager.Instance;
        }

        public event Action OnValueDirty;

        public override void OnStart()
        {
            HoldoutZoneModificationManager.Instance.RegisterModificationProvider(this);

            RoR2Application.onFixedUpdate += onFixedUpdate;
        }

        void onFixedUpdate()
        {
            OnValueDirty?.Invoke();
        }

        public override void OnEnd()
        {
            if (HoldoutZoneModificationManager.Instance)
            {
                HoldoutZoneModificationManager.Instance.UnregisterModificationProvider(this);
            }

            RoR2Application.onFixedUpdate -= onFixedUpdate;
        }

        public void ModifyValue(ref HoldoutZoneModificationInfo value)
        {
            value.RadiusMultiplier *= Mathf.Lerp(1f, 1f / 4f, _radiusInterpolateCurve.Evaluate(value.ZoneController.charge));
        }
    }
}
