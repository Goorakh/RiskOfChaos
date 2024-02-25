using BepInEx.Configuration;
using System;

namespace RiskOfChaos.ConfigHandling.AcceptableValues
{
    public class AcceptableValueMax<T> : AcceptableValueBase where T : IComparable
    {
        public T MaxValue { get; }

        public AcceptableValueMax(T maxValue) : base(typeof(T))
        {
            if (maxValue == null)
            {
                throw new ArgumentNullException(nameof(maxValue));
            }

            MaxValue = maxValue;
        }

        public override object Clamp(object value)
        {
            if (MaxValue.CompareTo(value) < 0)
            {
                return MaxValue;
            }

            return value;
        }

        public override bool IsValid(object value)
        {
            return MaxValue.CompareTo(value) >= 0;
        }

        public override string ToDescriptionString()
        {
            return $"# Acceptable value range: Less than or equal to {MaxValue}";
        }
    }
}
