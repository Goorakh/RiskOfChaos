using System;

namespace RiskOfChaos.ModifierController
{
    public interface IValueModificationManager<TModificationProvider, TValue> where TModificationProvider : IValueModificationProvider<TValue>
    {
        event Action OnValueModificationUpdated;

        bool AnyModificationActive { get; }

        void RegisterModificationProvider(TModificationProvider provider);

        void UnregisterModificationProvider(TModificationProvider provider);
    }
}
