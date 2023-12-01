using System;
using UnityEngine;

namespace RiskOfChaos.ModifierController
{
    public readonly record struct ModificationProviderInfo<T>(IValueModificationProvider<T> ModificationProvider,
                                                              ValueInterpolationFunctionType InterpolationType,
                                                              float InterpolationTime,
                                                              float TimeStarted) : IEquatable<IValueModificationProvider<T>>
    {
        public readonly float Age => Time.time - TimeStarted;

        public readonly bool IsInterpolating => Age <= InterpolationTime;

        public readonly bool Equals(ModificationProviderInfo<T> other)
        {
            return Equals(other.ModificationProvider);
        }

        public readonly bool Equals(IValueModificationProvider<T> other)
        {
            return ReferenceEquals(ModificationProvider, other);
        }

        public readonly override int GetHashCode()
        {
            return ModificationProvider.GetHashCode();
        }

        public override string ToString()
        {
            return $"{ModificationProvider} ({Age:F1})";
        }
    }
}
