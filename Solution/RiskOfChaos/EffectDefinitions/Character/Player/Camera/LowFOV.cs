using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.ModifierController.Camera;
using RiskOfChaos.Utilities.Interpolation;
using System;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Camera
{
    [ChaosTimedEffect("low_fov", 45f)]
    public sealed class LowFOV : TimedEffect, ICameraModificationProvider
    {
        [EffectCanActivate]
        static bool CanActivate()
        {
            return CameraModificationManager.Instance;
        }

        public event Action OnValueDirty;

        public void ModifyValue(ref CameraModificationData value)
        {
            value.FOVMultiplier *= 0.6f;
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
