using System;
using System.Collections.Generic;
using System.Text;

namespace RiskOfChaos.ConfigHandling
{
    public delegate bool ValueValidator<T>(T value);

    public static class CommonValueValidators
    {
        public static ValueValidator<T> None<T>()
        {
            return t => true;
        }

        public static ValueValidator<T> DefinedEnumValue<T>() where T : Enum
        {
            return t => Enum.IsDefined(typeof(T), t);
        }
    }
}
