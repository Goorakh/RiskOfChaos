using System;

namespace RiskOfChaos.ModifierController
{
    public interface IValueModificationManager<TValue>
    {
        event Action OnValueModificationUpdated;

        bool AnyModificationActive { get; }

        void RegisterModificationProvider(IValueModificationProvider<TValue> provider, ValueInterpolationFunctionType blendType, float valueInterpolationTime);

        void UnregisterModificationProvider(IValueModificationProvider<TValue> provider);

        void MarkValueModificationsDirty();

        void UpdateValueModifications();

        TValue InterpolateValue(in TValue a, in TValue b, float t, ValueInterpolationFunctionType interpolationType);

        TValue GetModifiedValue(TValue baseValue);
    }
}
