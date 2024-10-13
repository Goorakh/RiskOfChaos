using RiskOfChaos.OLD_ModifierController;
using RiskOfChaos.ScreenEffect;
using RiskOfChaos.Utilities.Interpolation;
using System;
using UnityEngine;

namespace RiskOfChaos.Utilities.CameraEffects
{
    [Obsolete]
    public class CameraEffect
    {
        public readonly InterpolationState InterpolationState = new InterpolationState();

        public ModificationProviderInterpolationDirection InterpolationDirection { get; private set; }

        public readonly Material Material;

        public readonly ScreenEffectType Type;

        readonly MaterialPropertyInterpolator _propertyInterpolator;

        public CameraEffect(Material material, MaterialPropertyInterpolator propertyInterpolator, ScreenEffectType type)
        {
            Material = material;
            _propertyInterpolator = propertyInterpolator;
            Type = type;
        }

        public void StartInterpolatingIn(ValueInterpolationFunctionType interpolationType, float duration)
        {
            _propertyInterpolator.SetPropertiesInterpolation(0f, Material);

            InterpolationState.StartInterpolating(interpolationType, duration, false);
            InterpolationDirection = ModificationProviderInterpolationDirection.In;
        }

        public void StartInterpolatingOut(ValueInterpolationFunctionType interpolationType, float duration)
        {
            _propertyInterpolator.SetPropertiesInterpolation(1f, Material);

            InterpolationState.StartInterpolating(interpolationType, duration, true);
            InterpolationDirection = ModificationProviderInterpolationDirection.Out;
        }

        public void OnInterpolationUpdate()
        {
            _propertyInterpolator.SetPropertiesInterpolation(InterpolationState.CurrentFraction, Material);
            InterpolationState.Update();
        }

        public void OnInterpolationFinished()
        {
            _propertyInterpolator.SetPropertiesInterpolation(InterpolationDirection == ModificationProviderInterpolationDirection.In ? 1f : 0f, Material);

            InterpolationDirection = ModificationProviderInterpolationDirection.None;
            InterpolationState.OnInterpolationFinished();
        }
    }
}
