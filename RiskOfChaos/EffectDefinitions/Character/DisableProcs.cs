using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.ModifierController.Damage;
using RoR2;
using System;

namespace RiskOfChaos.EffectDefinitions.Character
{
    [ChaosEffect("disable_procs")]
    [ChaosTimedEffect(45f, AllowDuplicates = false)]
    public sealed class DisableProcs : TimedEffect, IDamageInfoModificationProvider
    {
        [EffectCanActivate]
        static bool CanActivate()
        {
            return DamageInfoModificationManager.Instance;
        }

        public event Action OnValueDirty;

        public void ModifyValue(ref DamageInfo value)
        {
            value.procCoefficient = 0f;
        }

        public override void OnStart()
        {
            DamageInfoModificationManager.Instance.RegisterModificationProvider(this);
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
