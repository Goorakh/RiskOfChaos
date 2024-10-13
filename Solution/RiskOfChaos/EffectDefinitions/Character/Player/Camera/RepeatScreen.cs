using RiskOfChaos.ConfigHandling;
using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.OLD_ModifierController.UI;
using RiskOfChaos.ScreenEffect;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Assets;
using RiskOfChaos.Utilities.CameraEffects;
using RiskOfChaos.Utilities.Interpolation;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Camera
{
    [ChaosTimedEffect("repeat_screen", 90f)]
    public sealed class RepeatScreen : NetworkBehaviour, IUIModificationProvider
    {
        [EffectConfig]
        static readonly ConfigHolder<bool> _increaseHUDScale =
            ConfigFactory<bool>.CreateConfig("Increase HUD Scale", true)
                               .Description("Increases HUD scale slightly while the effect is active. Makes it easier to see the UI, but can also have scaling issues")
                               .OptionConfig(new CheckBoxConfig())
                               .Build();

        static readonly int _repeatCountID = Shader.PropertyToID("_RepeatCount");
        static readonly int _centerOffsetID = Shader.PropertyToID("_CenterOffset");

        static readonly Vector4 _centerOffset = new Vector4(0.5f, 0.5f, 0f, 0f);

        static ScreenEffectIndex _screenEffectIndex = ScreenEffectIndex.Invalid;
        static Material _screenMaterial;

        [SystemInitializer(typeof(ScreenEffectCatalog))]
        static void Init()
        {
            _screenEffectIndex = ScreenEffectCatalog.FindScreenEffectIndex("RepeatScreen");
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _screenEffectIndex != ScreenEffectIndex.Invalid;
        }

        Material _materialInstance;

        public event Action OnValueDirty;

        void Start()
        {
            if (NetworkClient.active)
            {
                _materialInstance = new Material(_screenMaterial);
                _materialInstance.SetVector(_centerOffsetID, _centerOffset);

                MaterialPropertyInterpolator propertyInterpolator = new MaterialPropertyInterpolator();
                propertyInterpolator.SetFloat(_repeatCountID, 1f, 3f);

                CameraEffectManager.AddEffect(_materialInstance, ScreenEffectType.UIAndWorld, propertyInterpolator, ValueInterpolationFunctionType.EaseInOut, 2f);

                if (UIModificationManager.Instance)
                {
                    UIModificationManager.Instance.RegisterModificationProvider(this, ValueInterpolationFunctionType.EaseInOut, 2f);
                }

                _increaseHUDScale.SettingChanged += onIncreaseHUDScaleChanged;
            }
        }

        void OnDestroy()
        {
            _increaseHUDScale.SettingChanged -= onIncreaseHUDScaleChanged;

            CameraEffectManager.RemoveEffect(_materialInstance, ValueInterpolationFunctionType.EaseInOut, 1f);

            if (UIModificationManager.Instance)
            {
                UIModificationManager.Instance.UnregisterModificationProvider(this, ValueInterpolationFunctionType.EaseInOut, 1f);
            }
        }

        void onIncreaseHUDScaleChanged(object sender, ConfigChangedArgs<bool> e)
        {
            OnValueDirty?.Invoke();
        }

        public void ModifyValue(ref UIModificationData value)
        {
            if (_increaseHUDScale.Value)
            {
                value.ScaleMultiplier *= 1.5f;
            }
        }
    }
}
