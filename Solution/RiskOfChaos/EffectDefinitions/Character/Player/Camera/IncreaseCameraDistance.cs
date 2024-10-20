using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.ModificationController;
using RiskOfChaos.ModificationController.Camera;
using RiskOfChaos.Utilities.Interpolation;
using RiskOfOptions.OptionConfigs;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Camera
{
    [ChaosTimedEffect("increase_camera_distance", 90f, ConfigName = "Increase Camera Distance")]
    public sealed class IncreaseCameraDistance : MonoBehaviour
    {
        [EffectConfig]
        static readonly ConfigHolder<float> _distanceMultiplier =
            ConfigFactory<float>.CreateConfig("Camera Distance Multiplier", 5f)
                                .AcceptableValues(new AcceptableValueMin<float>(1f))
                                .OptionConfig(new FloatFieldConfig { Min = 1f, FormatString = "{0}x" })
                                .Build();

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
                cameraModificationProvider.DistanceMultiplierConfigBinding.BindToConfig(_distanceMultiplier);

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
