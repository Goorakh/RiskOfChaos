using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.OLD_ModifierController.Cost;
using System;

namespace RiskOfChaos.EffectDefinitions.World.PurchaseInteractionCost
{
    [ChaosTimedEffect("everything_free", 30f, AllowDuplicates = false)]
    public sealed class EverythingFree : TimedEffect, ICostModificationProvider
    {
        [EffectCanActivate]
        static bool CanActivate()
        {
            return CostModificationManager.Instance;
        }

        public event Action OnValueDirty;

        public override void OnStart()
        {
            CostModificationManager.Instance.RegisterModificationProvider(this);
        }

        public override void OnEnd()
        {
            if (CostModificationManager.Instance)
            {
                CostModificationManager.Instance.UnregisterModificationProvider(this);
            }
        }

        public void ModifyValue(ref CostModificationInfo value)
        {
            value.CostMultiplier = 0f;
            value.AllowZeroCostResultOverride = true;
        }
    }
}
