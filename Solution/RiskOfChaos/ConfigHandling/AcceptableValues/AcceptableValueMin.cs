using BepInEx.Configuration;
using System;

namespace RiskOfChaos.ConfigHandling.AcceptableValues
{
    public class AcceptableValueMin<T> : AcceptableValueBase where T : IComparable
    {
        public T MinValue { get; }

        public AcceptableValueMin(T minValue) : base(typeof(T))
        {
            if (minValue == null)
            {
                throw new ArgumentNullException(nameof(minValue));
            }

            MinValue = minValue;
        }

        public override object Clamp(object value)
        {
            if (MinValue.CompareTo(value) > 0)
            {
                return MinValue;
            }

            return value;
        }

        public override bool IsValid(object value)
        {
            return MinValue.CompareTo(value) <= 0;
        }

        public override string ToDescriptionString()
        {
            return $"# Acceptable value range: Greater than or equal to {MinValue}";
        }
    }
}
