using System;
using RiskOfChaos.Utilities.Interpolation;

namespace RiskOfChaos.OLD_ModifierController
{
    public class ModificationProviderInfo<T> : IEquatable<IValueModificationProvider<T>>
    {
        public readonly IValueModificationProvider<T> ModificationProvider;

        public readonly InterpolationState InterpolationState = new InterpolationState();

        public ModificationProviderInterpolationDirection InterpolationDirection { get; private set; }

        public ModificationProviderInfo(IValueModificationProvider<T> modificationProvider)
        {
            ModificationProvider = modificationProvider;
        }

        public void StartInterpolatingIn(ValueInterpolationFunctionType interpolationType, float duration)
        {
            InterpolationState.StartInterpolating(interpolationType, duration, false);
            InterpolationDirection = ModificationProviderInterpolationDirection.In;
        }

        public void StartInterpolatingOut(ValueInterpolationFunctionType interpolationType, float duration)
        {
            InterpolationState.StartInterpolating(interpolationType, duration, true);
            InterpolationDirection = ModificationProviderInterpolationDirection.Out;
        }

        public void OnInterpolationUpdate()
        {
            InterpolationState.Update();
        }

        public void OnInterpolationFinished()
        {
            InterpolationDirection = ModificationProviderInterpolationDirection.None;
            InterpolationState.OnInterpolationFinished();
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
