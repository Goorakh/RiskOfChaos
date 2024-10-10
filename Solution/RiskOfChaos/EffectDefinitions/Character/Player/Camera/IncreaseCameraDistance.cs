using RiskOfChaos.ConfigHandling;
using RiskOfChaos.ConfigHandling.AcceptableValues;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.ModifierController.Camera;
using RiskOfChaos.Utilities.Interpolation;
using RiskOfOptions.OptionConfigs;
using System;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Camera
{
    [ChaosTimedEffect("increase_camera_distance", 90f, ConfigName = "Increase Camera Distance")]
    public sealed class IncreaseCameraDistance : NetworkBehaviour, ICameraModificationProvider
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
            return CameraModificationManager.Instance;
        }

        public event Action OnValueDirty;
        
        void Start()
        {
            if (NetworkServer.active)
            {
                CameraModificationManager.Instance.RegisterModificationProvider(this, ValueInterpolationFunctionType.EaseInOut, 1f);
                _distanceMultiplier.SettingChanged += onDistanceMultiplierChanged;
            }
        }

        void OnDestroy()
        {
            if (CameraModificationManager.Instance)
            {
                CameraModificationManager.Instance.UnregisterModificationProvider(this, ValueInterpolationFunctionType.EaseInOut, 1f);
            }

            _distanceMultiplier.SettingChanged -= onDistanceMultiplierChanged;
        }

        void onDistanceMultiplierChanged(object sender, ConfigChangedArgs<float> e)
        {
            OnValueDirty?.Invoke();
        }

        public void ModifyValue(ref CameraModificationData value)
        {
            value.CameraDistanceMultiplier += _distanceMultiplier.Value - 1f;
        }
    }
}
