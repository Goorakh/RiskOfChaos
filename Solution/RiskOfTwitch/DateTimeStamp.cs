using System;
using System.Text;

namespace RiskOfTwitch
{
    public readonly struct DateTimeStamp : IEquatable<DateTimeStamp>
    {
        public static DateTimeStamp Now => new DateTimeStamp(DateTime.Now);

        public readonly DateTime Time;

        public readonly TimeSpan TimeSince => DateTime.Now - Time;

        public readonly TimeSpan TimeUntil => Time - DateTime.Now;

        public readonly bool IsFuture => TimeUntil.Ticks > 0;

        public readonly bool HasPassed => TimeSince.Ticks > 0;

        public DateTimeStamp(DateTime time)
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
            return obj is DateTimeStamp stamp && Equals(stamp);
        }

        public bool Equals(DateTimeStamp other)
        {
            return Time == other.Time;
        }

        public override int GetHashCode()
        {
            return 615635108 + Time.GetHashCode();
        }

        public static implicit operator DateTimeStamp(DateTime time)
        {
            return new DateTimeStamp(time);
        }

        public static bool operator ==(DateTimeStamp left, DateTimeStamp right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DateTimeStamp left, DateTimeStamp right)
        {
            return !(left == right);
        }

        public static bool operator <(DateTimeStamp left, DateTimeStamp right)
        {
            return left.Time < right.Time;
        }
        public static bool operator <(DateTimeStamp left, DateTime right)
        {
            return left.Time < right;
        }
        public static bool operator <(DateTime left, DateTimeStamp right)
        {
            return left < right.Time;
        }

        public static bool operator >(DateTimeStamp left, DateTimeStamp right)
        {
            return left.Time > right.Time;
        }
        public static bool operator >(DateTimeStamp left, DateTime right)
        {
            return left.Time > right;
        }
        public static bool operator >(DateTime left, DateTimeStamp right)
        {
            return left > right.Time;
        }

        public static bool operator <=(DateTimeStamp left, DateTimeStamp right)
        {
            return left.Time <= right.Time;
        }
        public static bool operator <=(DateTimeStamp left, DateTime right)
        {
            return left.Time <= right;
        }
        public static bool operator <=(DateTime left, DateTimeStamp right)
        {
            return left <= right.Time;
        }

        public static bool operator >=(DateTimeStamp left, DateTimeStamp right)
        {
            return left.Time >= right.Time;
        }
        public static bool operator >=(DateTimeStamp left, DateTime right)
        {
            return left.Time >= right;
        }
        public static bool operator >=(DateTime left, DateTimeStamp right)
        {
            return left >= right.Time;
        }
    }
}
