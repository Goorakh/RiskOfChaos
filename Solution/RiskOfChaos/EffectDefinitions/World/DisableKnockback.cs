using RiskOfChaos.EffectHandling;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.ModifierController.Knockback;
using System;

namespace RiskOfChaos.EffectDefinitions.World
{
    [ChaosTimedEffect("disable_knockback", TimedEffectType.UntilStageEnd, AllowDuplicates = false, DefaultSelectionWeight = 0.8f)]
    public sealed class DisableKnockback : TimedEffect, IKnockbackModificationProvider
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

        public override void OnEnd()
        {
            if (KnockbackModificationManager.Instance)
            {
                KnockbackModificationManager.Instance.UnregisterModificationProvider(this);
            }
        }

        public event Action OnValueDirty;

        public void ModifyValue(ref float value)
        {
            value = 0f;
        }
    }
}
