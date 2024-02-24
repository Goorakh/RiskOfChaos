using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.ModifierController.Pickups;
using System;

namespace RiskOfChaos.EffectDefinitions.World.Pickups
{
    [ChaosTimedEffect("double_pickups", 90f)]
    public sealed class DoublePickups : TimedEffect, IPickupModificationProvider
    {
        [EffectCanActivate]
        static bool CanActivate()
        {
            return PickupModificationManager.Instance;
        }

        public event Action OnValueDirty;

        public override void OnStart()
        {
            PickupModificationManager.Instance.RegisterModificationProvider(this);
        }

        public override void OnEnd()
        {
            if (PickupModificationManager.Instance)
            {
                PickupModificationManager.Instance.UnregisterModificationProvider(this);
            }
        }

        public void ModifyValue(ref PickupModificationInfo value)
        {
            value.SpawnCountMultiplier *= 2;
        }
    }
}
