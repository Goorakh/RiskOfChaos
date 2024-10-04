using RiskOfChaos.ConfigHandling;
using RiskOfChaos.EffectHandling.Controllers;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.ModifierController.UI;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Assets;
using RiskOfChaos.Utilities.CameraEffects;
using RiskOfChaos.Utilities.Interpolation;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Camera
{
    [ChaosTimedEffect("repeat_screen", 90f, IsNetworked = true, AllowDuplicates = true)]
    public sealed class RepeatScreen : TimedEffect, IUIModificationProvider
    {
        [EffectConfig]
        static readonly ConfigHolder<bool> _increaseHUDScale =
            ConfigFactory<bool>.CreateConfig("Increase HUD Scale", true)
                               .Description("Increases HUD scale slightly while the effect is active. Makes it easier to see the UI, but can also have scaling issues")
                               .OptionConfig(new CheckBoxConfig())
                               .OnValueChanged(() =>
                               {
                                   if (!ChaosEffectTracker.Instance)
                                       return;

                                   ChaosEffectTracker.Instance.OLD_InvokeEventOnAllInstancesOfEffect<RepeatScreen>(e => e.OnValueDirty);
                               })
                               .Build();

        static readonly int _repeatCountID = Shader.PropertyToID("_RepeatCount");
        static readonly int _centerOffsetID = Shader.PropertyToID("_CenterOffset");

        static readonly Vector4 _centerOffset = new Vector4(0.5f, 0.5f, 0f, 0f);

        static Material _screenMaterial;

        [SystemInitializer]
        static void Init()
        {
            _screenMaterial = AssetLoader.LoadAssetCached<Material>("riskofchaos", "RepeatScreen");
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _screenMaterial;
        }

        Material _materialInstance;

        public event Action OnValueDirty;

        public override void OnStart()
        {
            _materialInstance = new Material(_screenMaterial);
            _materialInstance.SetVector(_centerOffsetID, _centerOffset);

            MaterialPropertyInterpolator propertyInterpolator = new MaterialPropertyInterpolator();
            propertyInterpolator.SetFloat(_repeatCountID, 1f, 3f);

            CameraEffectManager.AddEffect(_materialInstance, CameraEffectType.UIAndWorld, propertyInterpolator, ValueInterpolationFunctionType.EaseInOut, 2f);

            if (UIModificationManager.Instance)
            {
                UIModificationManager.Instance.RegisterModificationProvider(this, ValueInterpolationFunctionType.EaseInOut, 2f);
            }
        }

        public void ModifyValue(ref UIModificationData value)
        {
            if (_increaseHUDScale.Value)
            {
                value.ScaleMultiplier *= 1.65f;
            }
        }

        public override void OnEnd()
        {
            CameraEffectManager.RemoveEffect(_materialInstance, ValueInterpolationFunctionType.EaseInOut, 1f);

            if (UIModificationManager.Instance)
            {
                UIModificationManager.Instance.UnregisterModificationProvider(this, ValueInterpolationFunctionType.EaseInOut, 1f);
            }
        }
    }
}
