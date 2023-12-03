using System;

namespace RiskOfChaos.ModifierController
{
    public class ModificationProviderInfo<T> : IEquatable<IValueModificationProvider<T>>
    {
        public readonly IValueModificationProvider<T> ModificationProvider;

        InterpolationState _interpolationState;
        public InterpolationState InterpolationState => _interpolationState;

        public ModificationProviderInterpolationDirection InterpolationDirection { get; private set; }

        public ModificationProviderInfo(IValueModificationProvider<T> modificationProvider)
        {
            ModificationProvider = modificationProvider;
        }

        public void StartInterpolatingIn(ValueInterpolationFunctionType interpolationType, float duration)
        {
            _interpolationState.StartInterpolating(interpolationType, duration, false);
            InterpolationDirection = ModificationProviderInterpolationDirection.In;
        }

        public void StartInterpolatingOut(ValueInterpolationFunctionType interpolationType, float duration)
        {
            _interpolationState.StartInterpolating(interpolationType, duration, true);
            InterpolationDirection = ModificationProviderInterpolationDirection.Out;
        }

        public void OnInterpolationFinished()
        {
            InterpolationDirection = ModificationProviderInterpolationDirection.None;
        }

        public bool Equals(ModificationProviderInfo<T> other)
        {
            return Equals(other.ModificationProvider);
        }

        public bool Equals(IValueModificationProvider<T> other)
        {
            return ReferenceEquals(ModificationProvider, other);
        }

        public override int GetHashCode()
        {
            return ModificationProvider.GetHashCode();
        }

        public override string ToString()
        {
            return ModificationProvider.ToString();
        }
    }
}
