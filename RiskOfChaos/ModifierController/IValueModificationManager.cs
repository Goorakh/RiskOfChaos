using RiskOfChaos.Utilities.Interpolation;
using System;

namespace RiskOfChaos.ModifierController
{
    public interface IValueModificationManager<TValue>
    {
        event Action OnValueModificationUpdated;

        bool AnyModificationActive { get; }

        void ClearAllModificationProviders();

        InterpolationState RegisterModificationProvider(IValueModificationProvider<TValue> provider, ValueInterpolationFunctionType blendType, float valueInterpolationTime);

        InterpolationState UnregisterModificationProvider(IValueModificationProvider<TValue> provider, ValueInterpolationFunctionType blendType, float valueInterpolationTime);

        void MarkValueModificationsDirty();

        void UpdateValueModifications();

        TValue InterpolateValue(in TValue a, in TValue b, float t);

        TValue GetModifiedValue(TValue baseValue);
    }
}
