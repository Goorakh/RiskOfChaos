using System;
using System.Collections.Generic;
using System.Text;

namespace RiskOfChaos.ModifierController
{
    public interface IValueModificationProvider<T>
    {
        event Action OnValueDirty;

        void ModifyValue(ref T value);
    }
}
