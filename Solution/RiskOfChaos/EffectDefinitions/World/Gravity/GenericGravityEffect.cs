using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.ModifierController.Gravity;
using System;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.World.Gravity
{
    public abstract class GenericGravityEffect : TimedEffect, IGravityModificationProvider
    {
        [EffectCanActivate]
        static bool CanActivate()
        {
            return GravityModificationManager.Instance;
        }

        public abstract event Action OnValueDirty;

        public abstract void ModifyValue(ref Vector3 gravity);

        public override void OnStart()
        {
            if (NetworkServer.active)
            {
                GravityModificationManager.Instance.RegisterModificationProvider(this);
            }
        }

        public override void OnEnd()
        {
            if (NetworkServer.active && GravityModificationManager.Instance)
            {
                GravityModificationManager.Instance.UnregisterModificationProvider(this);
            }
        }
    }
}
