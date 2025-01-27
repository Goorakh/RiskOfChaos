﻿using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.ModificationController;
using RiskOfChaos.ModificationController.Camera;
using RiskOfChaos.Utilities.Interpolation;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Camera
{
    [ChaosTimedEffect("high_fov", 90f)]
    public sealed class HighFOV : MonoBehaviour
    {
        [EffectCanActivate]
        static bool CanActivate()
        {
            return RoCContent.NetworkedPrefabs.CameraModificationProvider;
        }

        ValueModificationController _cameraModificationController;

        void Start()
        {
            if (NetworkServer.active)
            {
                _cameraModificationController = Instantiate(RoCContent.NetworkedPrefabs.CameraModificationProvider).GetComponent<ValueModificationController>();
                _cameraModificationController.SetInterpolationParameters(new InterpolationParameters(1f));

                CameraModificationProvider cameraModificationProvider = _cameraModificationController.GetComponent<CameraModificationProvider>();
                cameraModificationProvider.FOVMultiplier = 1.75f;

                NetworkServer.Spawn(_cameraModificationController.gameObject);
            }
        }

        void OnDestroy()
        {
            if (_cameraModificationController)
            {
                _cameraModificationController.Retire();
                _cameraModificationController = null;
            }
        }
    }
}
