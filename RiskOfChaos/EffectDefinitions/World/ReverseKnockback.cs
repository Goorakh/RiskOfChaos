using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.ModifierController.Knockback;
using System;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosTimedEffect("reverse_knockback", TimedEffectType.UntilStageEnd, EffectWeightReductionPercentagePerActivation = 0.3f)]
    public sealed class ReverseKnockback : TimedEffect, IKnockbackModificationProvider
    {
        [EffectCanActivate]
        static bool CanActivate()
        {
            return KnockbackModificationManager.Instance;
        }

        public override void OnStart()
        {
            KnockbackModificationManager.Instance.RegisterModificationProvider(this);
        }

        public event Action OnValueDirty;

        public void ModifyValue(ref float value)
        {
            value *= -1f;
        }

        public override void OnEnd()
        {
            if (KnockbackModificationManager.Instance)
            {
                KnockbackModificationManager.Instance.UnregisterModificationProvider(this);
            }
        }
    }
}
