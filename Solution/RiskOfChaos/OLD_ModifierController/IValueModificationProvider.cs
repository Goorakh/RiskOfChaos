using System;

namespace RiskOfChaos.OLD_ModifierController
{
    public interface IValueModificationProvider<T>
    {
        event Action OnValueDirty;

        void ModifyValue(ref T value);
    }
}
