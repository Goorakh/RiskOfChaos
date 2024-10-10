using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.ModifierController.Camera;
using RiskOfChaos.Utilities.Interpolation;
using System;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Camera
{
    [ChaosTimedEffect("flip_camera", 30f, AllowDuplicates = false)]
    public sealed class FlipCamera : NetworkBehaviour, ICameraModificationProvider
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
            value.RotationOffset *= Quaternion.Euler(0f, 0f, 180f);
        }
    }
}
