using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.ModifierController.Damage;
using RoR2;
using System;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosEffect("disable_fall_damage")]
    [ChaosTimedEffect(TimedEffectType.UntilStageEnd, AllowDuplicates = false)]
    public sealed class DisableFallDamage : TimedEffect, IDamageInfoModificationProvider
    {
        public event Action OnValueDirty;

        [EffectCanActivate]
        static bool CanActivate()
        {
            return DamageInfoModificationManager.Instance;
        }

        public override void OnStart()
        {
            DamageInfoModificationManager.Instance.RegisterModificationProvider(this);
        }

        public void ModifyValue(ref DamageInfo value)
        {
            if ((value.damageType & DamageType.FallDamage) != 0)
            {
                value.damage = 0f;
                value.rejected = true;
                value.canRejectForce = true;
            }
        }

        public override void OnEnd()
        {
            if (DamageInfoModificationManager.Instance)
            {
                DamageInfoModificationManager.Instance.UnregisterModificationProvider(this);
            }
        }
    }
}
