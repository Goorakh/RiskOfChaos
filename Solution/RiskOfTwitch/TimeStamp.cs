using System;
using System.Text;

namespace RiskOfTwitch
{
    public readonly struct TimeStamp : IEquatable<TimeStamp>
    {
        public static TimeStamp Now => new TimeStamp(DateTime.Now);

        public readonly DateTime Time;

        public readonly TimeSpan TimeSince => DateTime.Now - Time;

        public readonly TimeSpan TimeUntil => Time - DateTime.Now;

        public readonly bool IsFuture => TimeUntil.Ticks > 0;

        public readonly bool HasPassed => TimeSince.Ticks > 0;

        public TimeStamp(DateTime time)
        {
            Time = time;
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append(Time.ToString());

            if (IsFuture)
            {
                stringBuilder.Append($" (in {TimeUntil})");
            }
            else
            {
                stringBuilder.Append($" ({TimeSince} ago)");
            }

            return stringBuilder.ToString();
        }

        public override bool Equals(object obj)
        {
            return obj is TimeStamp stamp && Equals(stamp);
        }

        public bool Equals(TimeStamp other)
        {
            return Time == other.Time;
        }

        public override int GetHashCode()
        {
            return 615635108 + Time.GetHashCode();
        }

        public static implicit operator TimeStamp(DateTime time)
        {
            return new TimeStamp(time);
        }

        public static bool operator ==(TimeStamp left, TimeStamp right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TimeStamp left, TimeStamp right)
        {
            return !(left == right);
        }

        public static bool operator <(TimeStamp left, TimeStamp right)
        {
            return left.Time < right.Time;
        }
        public static bool operator <(TimeStamp left, DateTime right)
        {
            return left.Time < right;
        }
        public static bool operator <(DateTime left, TimeStamp right)
        {
            return left < right.Time;
        }

        public static bool operator >(TimeStamp left, TimeStamp right)
        {
            return left.Time > right.Time;
        }
        public static bool operator >(TimeStamp left, DateTime right)
        {
            return left.Time > right;
        }
        public static bool operator >(DateTime left, TimeStamp right)
        {
            return left > right.Time;
        }

        public static bool operator <=(TimeStamp left, TimeStamp right)
        {
            return left.Time <= right.Time;
        }
        public static bool operator <=(TimeStamp left, DateTime right)
        {
            return left.Time <= right;
        }
        public static bool operator <=(DateTime left, TimeStamp right)
        {
            return left <= right.Time;
        }

        public static bool operator >=(TimeStamp left, TimeStamp right)
        {
            return left.Time >= right.Time;
        }
        public static bool operator >=(TimeStamp left, DateTime right)
        {
            return left.Time >= right;
        }
        public static bool operator >=(DateTime left, TimeStamp right)
        {
            return left >= right.Time;
        }
    }
}
