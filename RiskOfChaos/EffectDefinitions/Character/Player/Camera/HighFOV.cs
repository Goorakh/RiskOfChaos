using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.ModifierController;
using RiskOfChaos.ModifierController.Camera;
using System;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Camera
{
    [ChaosTimedEffect("high_fov", 90f)]
    public sealed class HighFOV : TimedEffect, ICameraModificationProvider
    {
        [EffectCanActivate]
        static bool CanActivate()
        {
            return CameraModificationManager.Instance;
        }

        public event Action OnValueDirty;

        public void ModifyValue(ref CameraModificationData value)
        {
            value.FOVMultiplier *= 2f;
        }

        public override void OnStart()
        {
            CameraModificationManager.Instance.RegisterModificationProvider(this, ValueInterpolationFunctionType.EaseInOut, 1f);
        }

        public override void OnEnd()
        {
            if (CameraModificationManager.Instance)
            {
                CameraModificationManager.Instance.UnregisterModificationProvider(this, ValueInterpolationFunctionType.EaseInOut, 1f);
            }
        }
    }
}
