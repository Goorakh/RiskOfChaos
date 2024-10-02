using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.ModifierController.Camera;
using System;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Camera
{
    [ChaosTimedEffect("disable_recoil", 90f, AllowDuplicates = false)]
    public sealed class DisableRecoil : TimedEffect, ICameraModificationProvider
    {
        [EffectCanActivate]
        static bool CanActivate()
        {
            return CameraModificationManager.Instance;
        }

        public event Action OnValueDirty;

        public void ModifyValue(ref CameraModificationData value)
        {
            value.RecoilMultiplier = Vector2.zero;
        }

        public override void OnStart()
        {
            CameraModificationManager.Instance.RegisterModificationProvider(this);
        }

        public override void OnEnd()
        {
            if (CameraModificationManager.Instance)
            {
                CameraModificationManager.Instance.UnregisterModificationProvider(this);
            }
        }
    }
}
