using System;

namespace RiskOfChaos.ConfigHandling
{
    public delegate T ValueConstrictor<T>(T value);

    public static class CommonValueConstrictors
    {
        public static ValueConstrictor<T> None<T>()
        {
            return t => t;
        }

        public static ValueConstrictor<T> GreaterThanOrEqualTo<T>(T value) where T : IComparable
        {
            return t => t.CompareTo(value) >= 0 ? t : value;
        }

        public static ValueConstrictor<T> LessThanOrEqualTo<T>(T value) where T : IComparable
        {
            return t => t.CompareTo(value) <= 0 ? t : value;
        }

        public static readonly ValueConstrictor<float> Clamped01Float = Clamped(0f, 1f);

        public static ValueConstrictor<T> Clamped<T>(T min, T max) where T : IComparable
        {
            return t =>
            {
                if (t is null)
                    return t;

                if (t.CompareTo(min) < 0)
                {
                    return min;
                }
                else if (t.CompareTo(max) > 0)
                {
                    return max;
                }
                else
                {
                    return t;
                }
            };
        }
    }
}
