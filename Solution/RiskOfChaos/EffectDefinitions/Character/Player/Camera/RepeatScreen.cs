using RiskOfChaos.Components;
using RiskOfChaos.Components.MaterialInterpolation;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.OLD_ModifierController.UI;
using RiskOfChaos.ScreenEffect;
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

        static readonly Vector4 _centerOffset = new Vector4(0.5f, 0.5f, 0f, 0f);

        static ScreenEffectIndex _screenEffectIndex = ScreenEffectIndex.Invalid;

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

        ScreenEffectComponent _screenEffect;

        public event Action OnValueDirty;

        void Start()
        {
            if (NetworkServer.active)
            {
                _screenEffect = Instantiate(RoCContent.NetworkedPrefabs.InterpolatedScreenEffect).GetComponent<ScreenEffectComponent>();

                NetworkedMaterialPropertyInterpolators screenEffectPropertyInterpolators = _screenEffect.GetComponent<NetworkedMaterialPropertyInterpolators>();
                screenEffectPropertyInterpolators.PropertyInterpolations.Add(new MaterialPropertyInterpolationData("_CenterOffset", _centerOffset));
                screenEffectPropertyInterpolators.PropertyInterpolations.Add(new MaterialPropertyInterpolationData("_RepeatCount", 1f, 3f));

                GenericInterpolationComponent screenEffectInterpolation = _screenEffect.GetComponent<GenericInterpolationComponent>();
                screenEffectInterpolation.InterpolationIn = new InterpolationParameters(2f);
                screenEffectInterpolation.InterpolationOut = new InterpolationParameters(1f);

                _screenEffect.ScreenEffectIndex = _screenEffectIndex;

                NetworkServer.Spawn(_screenEffect.gameObject);
            }

            if (UIModificationManager.Instance)
            {
                UIModificationManager.Instance.RegisterModificationProvider(this, ValueInterpolationFunctionType.EaseInOut, 2f);
            }

            if (NetworkClient.active)
            {
                _increaseHUDScale.SettingChanged += onIncreaseHUDScaleChanged;
            }
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            refreshHudScalingEnabled();
        }

        void OnDestroy()
        {
            _increaseHUDScale.SettingChanged -= onIncreaseHUDScaleChanged;

            if (_screenEffect)
            {
                _screenEffect.Remove();
            }

            if (UIModificationManager.Instance)
            {
                UIModificationManager.Instance.UnregisterModificationProvider(this, ValueInterpolationFunctionType.EaseInOut, 1f);
            }
        }

        void onIncreaseHUDScaleChanged(object sender, ConfigChangedArgs<bool> e)
        {
            refreshHudScalingEnabled();
        }

        [Client]
        void refreshHudScalingEnabled()
        {

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
