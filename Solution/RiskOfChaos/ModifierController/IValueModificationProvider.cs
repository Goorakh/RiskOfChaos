using System;

namespace RiskOfChaos.ModifierController
{
    public interface IValueModificationProvider<T>
    {
        event Action OnValueDirty;

        void ModifyValue(ref T value);
    }
}
