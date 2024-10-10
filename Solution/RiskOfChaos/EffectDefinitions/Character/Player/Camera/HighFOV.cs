using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.ModifierController.Camera;
using RiskOfChaos.Utilities.Interpolation;
using System;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Camera
{
    [ChaosTimedEffect("high_fov", 90f)]
    public sealed class HighFOV : NetworkBehaviour, ICameraModificationProvider
    {
        [EffectCanActivate]
        static bool CanActivate()
        {
            return CameraModificationManager.Instance;
        }

        public event Action OnValueDirty;

        void Start()
        {
            if (NetworkServer.active)
            {
                CameraModificationManager.Instance.RegisterModificationProvider(this, ValueInterpolationFunctionType.EaseInOut, 1f);
            }
        }

        void OnDestroy()
        {
            if (CameraModificationManager.Instance)
            {
                CameraModificationManager.Instance.UnregisterModificationProvider(this, ValueInterpolationFunctionType.EaseInOut, 1f);
            }
        }

        public void ModifyValue(ref CameraModificationData value)
        {
            value.FOVMultiplier *= 1.75f;
        }
    }
}
