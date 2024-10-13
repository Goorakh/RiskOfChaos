using System;

namespace RiskOfChaos.Utilities.Interpolation
{
    public readonly struct InterpolationParameters : IEquatable<InterpolationParameters>
    {
        public static readonly InterpolationParameters None = new InterpolationParameters(0f);

        public readonly float Duration;

        public InterpolationParameters(float duration)
        {
            Duration = duration;
        }

        public override readonly bool Equals(object obj)
        {
            return obj is InterpolationParameters other && Equals(other);
        }

        public readonly bool Equals(InterpolationParameters other)
        {
            return Duration == other.Duration;
        }

        public override readonly int GetHashCode()
        {
            return Duration.GetHashCode();
        }

        public static bool operator ==(InterpolationParameters left, InterpolationParameters right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(InterpolationParameters left, InterpolationParameters right)
        {
            return !(left == right);
        }
    }
}
