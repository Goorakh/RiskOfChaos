using RiskOfChaos.Components;
using RiskOfChaos.Components.MaterialInterpolation;
using RiskOfChaos.ConfigHandling;
using RiskOfChaos.Content;
using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Data;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.ModificationController;
using RiskOfChaos.ModificationController.UI;
using RiskOfChaos.ScreenEffect;
using RiskOfChaos.Utilities.Interpolation;
using RiskOfOptions.OptionConfigs;
using RoR2;
using System;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Camera
{
    [ChaosTimedEffect("repeat_screen", 45f)]
    public sealed class RepeatScreen : MonoBehaviour
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
            if (_screenEffectIndex == ScreenEffectIndex.Invalid)
            {
                Log.Error("Failed to find screen effect index");
            }
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _screenEffectIndex != ScreenEffectIndex.Invalid;
        }

        ScreenEffectComponent _screenEffect;

        ValueModificationController _uiModificationController;

        public event Action OnValueDirty;

        void Start()
        {
            InterpolationParameters interpolationIn = new InterpolationParameters(2f);
            InterpolationParameters interpolationOut = new InterpolationParameters(1f);

            if (NetworkServer.active)
            {
                _screenEffect = Instantiate(RoCContent.NetworkedPrefabs.InterpolatedScreenEffect).GetComponent<ScreenEffectComponent>();

                NetworkedMaterialPropertyInterpolators screenEffectPropertyInterpolators = _screenEffect.GetComponent<NetworkedMaterialPropertyInterpolators>();
                screenEffectPropertyInterpolators.PropertyInterpolations.Add(new MaterialPropertyInterpolationData("_CenterOffset", _centerOffset));
                screenEffectPropertyInterpolators.PropertyInterpolations.Add(new MaterialPropertyInterpolationData("_RepeatCount", 1f, 3f));

                IInterpolationProvider screenEffectInterpolation = _screenEffect.GetComponent<IInterpolationProvider>();
                screenEffectInterpolation.InterpolationIn = interpolationIn;
                screenEffectInterpolation.InterpolationOut = interpolationOut;

                _screenEffect.ScreenEffectIndex = _screenEffectIndex;

                NetworkServer.Spawn(_screenEffect.gameObject);
            }

            if (NetworkClient.active)
            {
                _uiModificationController = Instantiate(RoCContent.LocalPrefabs.UIModificationProvider).GetComponent<ValueModificationController>();

                UIModificationProvider uiModificationProvider = _uiModificationController.GetComponent<UIModificationProvider>();
                uiModificationProvider.HudScaleMultiplierConfigBinding.BindToConfigConverted(_increaseHUDScale, v => v ? 1.5f : 1f);

                if (_uiModificationController.TryGetComponent(out IInterpolationProvider interpolationProvider))
                {
                    interpolationProvider.InterpolationIn = interpolationIn;
                    interpolationProvider.InterpolationOut = interpolationOut;
                }
            }
        }

        void OnDestroy()
        {
            if (_screenEffect)
            {
                _screenEffect.Remove();
                _screenEffect = null;
            }

            if (_uiModificationController)
            {
                _uiModificationController.Retire();
                _uiModificationController = null;
            }
        }
    }
}
