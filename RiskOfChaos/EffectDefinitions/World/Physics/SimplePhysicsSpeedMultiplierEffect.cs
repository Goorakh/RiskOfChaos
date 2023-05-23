using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.ModifierController.PhysicsModification;
using System;

namespace RiskOfChaos.EffectDefinitions.World.Physics
{
    public abstract class SimplePhysicsSpeedMultiplierEffect : TimedEffect, IPhysicsModificationProvider
    {
        [EffectCanActivate]
        static bool CanActivate()
        {
            return PhysicsModificationManager.Instance;
        }

        protected abstract float multiplier { get; }

        public override void OnStart()
        {
            PhysicsModificationManager.Instance.RegisterModificationProvider(this);
        }

        public event Action OnValueDirty;

        protected void invokeOnValueDirty()
        {
            OnValueDirty?.Invoke();
        }

        public void ModifyValue(ref PhysicsModificationInfo value)
        {
            value.SpeedMultiplier *= multiplier;
        }

        public override void OnEnd()
        {
            if (PhysicsModificationManager.Instance)
            {
                PhysicsModificationManager.Instance.UnregisterModificationProvider(this);
            }
        }
    }
}
