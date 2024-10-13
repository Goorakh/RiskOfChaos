using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.OLD_ModifierController.Projectile;
using System;

namespace RiskOfChaos.EffectDefinitions.World.ProjectileSpeed
{
    public abstract class GenericProjectileSpeedEffect : TimedEffect, IProjectileModificationProvider
    {
        [EffectCanActivate]
        static bool CanActivate()
        {
            return ProjectileModificationManager.Instance;
        }

        protected abstract float speedMultiplier { get; }

        public abstract event Action OnValueDirty;

        public void ModifyValue(ref ProjectileModificationData value)
        {
            value.SpeedMultiplier *= speedMultiplier;
        }

        public override void OnStart()
        {
            ProjectileModificationManager.Instance.RegisterModificationProvider(this);
        }

        public override void OnEnd()
        {
            if (ProjectileModificationManager.Instance)
            {
                ProjectileModificationManager.Instance.UnregisterModificationProvider(this);
            }
        }
    }
}
