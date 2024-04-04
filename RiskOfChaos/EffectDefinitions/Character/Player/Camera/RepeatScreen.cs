using RiskOfChaos.EffectHandling.EffectClassAttributes;
using RiskOfChaos.EffectHandling.EffectClassAttributes.Methods;
using RiskOfChaos.Utilities;
using RiskOfChaos.Utilities.Assets;
using RiskOfChaos.Utilities.CameraEffects;
using RiskOfChaos.Utilities.Interpolation;
using RoR2;
using UnityEngine;

namespace RiskOfChaos.EffectDefinitions.Character.Player.Camera
{
    [ChaosTimedEffect("repeat_screen", 90f, IsNetworked = true, AllowDuplicates = true)]
    public sealed class RepeatScreen : TimedEffect
    {
        static readonly int _repeatCountID = Shader.PropertyToID("_RepeatCount");
        static readonly int _centerOffsetID = Shader.PropertyToID("_CenterOffset");

        static readonly Vector4 _centerOffset = new Vector4(0.5f, 0.5f, 0f, 0f);

        static Material _screenMaterial;

        [SystemInitializer]
        static void Init()
        {
            _screenMaterial = AssetLoader.LoadAssetCached<Material>("assets", "RepeatScreen");
        }

        [EffectCanActivate]
        static bool CanActivate()
        {
            return _screenMaterial;
        }

        Material _materialInstance;

        public override void OnStart()
        {
            _materialInstance = new Material(_screenMaterial);
            _materialInstance.SetVector(_centerOffsetID, _centerOffset);

            MaterialPropertyInterpolator propertyInterpolator = new MaterialPropertyInterpolator();
            propertyInterpolator.SetFloat(_repeatCountID, 1f, 3f);

            CameraEffectManager.AddEffect(_materialInstance, propertyInterpolator, ValueInterpolationFunctionType.EaseInOut, 2f);
        }

        public override void OnEnd()
        {
            CameraEffectManager.RemoveEffect(_materialInstance, ValueInterpolationFunctionType.EaseInOut, 1f);
        }
    }
}
