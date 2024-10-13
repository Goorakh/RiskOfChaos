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
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Camera
{
    [ChaosTimedEffect("increase_camera_distance", 90f, ConfigName = "Increase Camera Distance")]
    public sealed class IncreaseCameraDistance : NetworkBehaviour
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
        CameraModificationProvider _cameraModificationProvider;
        
        void Start()
        {
            if (NetworkServer.active)
            {
                _cameraModificationController = Instantiate(RoCContent.NetworkedPrefabs.CameraModificationProvider).GetComponent<ValueModificationController>();
                _cameraModificationController.SetInterpolationParameters(new InterpolationParameters(1f));

                _cameraModificationProvider = _cameraModificationController.GetComponent<CameraModificationProvider>();

                updateCameraDistance();

                NetworkServer.Spawn(_cameraModificationController.gameObject);

                _distanceMultiplier.SettingChanged += onDistanceMultiplierChanged;
            }
        }

        void OnDestroy()
        {
            if (_cameraModificationController)
            {
                _cameraModificationController.Retire();
                _cameraModificationController = null;
                _cameraModificationProvider = null;
            }

            _distanceMultiplier.SettingChanged -= onDistanceMultiplierChanged;
        }

        void onDistanceMultiplierChanged(object sender, ConfigChangedArgs<float> e)
        {
            updateCameraDistance();
        }

        [Server]
        void updateCameraDistance()
        {
            if (_cameraModificationProvider)
            {
                _cameraModificationProvider.DistanceMultiplier = _distanceMultiplier.Value;
            }
        }
    }
}
