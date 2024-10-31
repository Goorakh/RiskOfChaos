using Newtonsoft.Json;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.Utilities.Extensions;
using RoR2;
using System;
using UnityEngine;

namespace RiskOfChaos.Utilities
{
    [Serializable]
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public struct RunTimeStamp : IEquatable<RunTimeStamp>, IComparable<RunTimeStamp>
    {
        [JsonProperty("t")]
        public RunTimerType TimeType;

        [JsonProperty("v")]
        public float Time;

        readonly float currentTime
        {
            get
            {
                Run run = Run.instance;
                if (!run)
                {
                    Log.Warning("no run instance");
                    return 0f;
                }

                return run.GetRunTime(TimeType);
            }
        }

        public readonly float TimeUntil => Time - currentTime;

        public readonly float TimeSince => currentTime - Time;

        public readonly float TimeUntilClamped => Mathf.Max(0f, TimeUntil);

        public readonly float TimeSinceClamped => Mathf.Max(0f, TimeSince);

        public readonly bool HasPassed => currentTime >= Time;

        public readonly bool IsInfinity => float.IsInfinity(Time);

        public readonly bool IsPositiveInfinity => float.IsPositiveInfinity(Time);

        public readonly bool IsNegativeInfinity => float.IsNegativeInfinity(Time);

        public RunTimeStamp(RunTimerType timeType, float time)
        {
            TimeType = timeType;
            Time = time;
        }

        public static RunTimeStamp Now(RunTimerType timeType)
        {
            float tNow;

            Run run = Run.instance;
            if (run)
            {
                tNow = run.GetRunTime(timeType);
            }
            else
            {
                Log.Warning("no run instance");
                tNow = 0f;
            }

            return new RunTimeStamp(timeType, tNow);
        }

        public readonly RunTimeStamp ConvertTo(RunTimerType timeType)
        {
            if (TimeType == timeType)
                return this;

            Run run = Run.instance;
            if (!run)
            {
                Log.Warning("No run instance");
                return new RunTimeStamp(timeType, Time);
            }

            Run.RunStopwatch runStopwatch = run.runStopwatch;

            switch (timeType)
            {
                case RunTimerType.Stopwatch:
                {
                    float time = Time;
                    if (runStopwatch.isPaused)
                        time -= Run.FixedTimeStamp.now.t;

                    return new RunTimeStamp(timeType, time + runStopwatch.offsetFromFixedTime);
                }
                case RunTimerType.Realtime:
                {
                    float time = Time - runStopwatch.offsetFromFixedTime;
                    if (runStopwatch.isPaused)
                        time += Run.FixedTimeStamp.now.t;

                    return new RunTimeStamp(timeType, time);
                }
                default:
                    throw new NotImplementedException($"RunTimerType {timeType} is not implemented");
            }
        }

        public override readonly string ToString()
        {
            return $"{Time} ({TimeType})";
        }

        public override readonly bool Equals(object obj)
        {
            return obj is RunTimeStamp stamp && Equals(stamp);
        }

        public readonly bool Equals(RunTimeStamp other)
        {
            return Time == other.ConvertTo(TimeType).Time;
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(TimeType, Time);
        }

        public readonly int CompareTo(RunTimeStamp other)
        {
            return Time.CompareTo(other.Time);
        }

        public static bool operator ==(RunTimeStamp left, RunTimeStamp right)
        {
            return left.Equals(right);
        }
        public static bool operator ==(RunTimeStamp left, float right)
        {
            return left.Time == right;
        }

        public static bool operator !=(RunTimeStamp left, RunTimeStamp right)
        {
            return !left.Equals(right);
        }
        public static bool operator !=(RunTimeStamp left, float right)
        {
            return left.Time != right;
        }

        public static bool operator <(RunTimeStamp left, RunTimeStamp right)
        {
            return left.Time < right.ConvertTo(left.TimeType).Time;
        }
        public static bool operator <(float left, RunTimeStamp right)
        {
            return left < right.Time;
        }
        public static bool operator <(RunTimeStamp left, float right)
        {
            return left.Time < right;
        }

        public static bool operator <=(RunTimeStamp left, RunTimeStamp right)
        {
            return left.Time <= right.ConvertTo(left.TimeType).Time;
        }
        public static bool operator <=(float left, RunTimeStamp right)
        {
            return left <= right.Time;
        }
        public static bool operator <=(RunTimeStamp left, float right)
        {
            return left.Time <= right;
        }

        public static bool operator >(RunTimeStamp left, RunTimeStamp right)
        {
            return left.Time > right.ConvertTo(left.TimeType).Time;
        }
        public static bool operator >(float left, RunTimeStamp right)
        {
            return left > right.Time;
        }
        public static bool operator >(RunTimeStamp left, float right)
        {
            return left.Time > right;
        }

        public static bool operator >=(RunTimeStamp left, RunTimeStamp right)
        {
            return left.Time >= right.ConvertTo(left.TimeType).Time;
        }
        public static bool operator >=(float left, RunTimeStamp right)
        {
            return left >= right.Time;
        }
        public static bool operator >=(RunTimeStamp left, float right)
        {
            return left.Time >= right;
        }

        public static RunTimeStamp operator +(RunTimeStamp left, RunTimeStamp right)
        {
            return new RunTimeStamp(left.TimeType, left.Time + right.ConvertTo(left.TimeType).Time);
        }
        public static RunTimeStamp operator +(RunTimeStamp left, float right)
        {
            return new RunTimeStamp(left.TimeType, left.Time + right);
        }

        public static RunTimeStamp operator -(RunTimeStamp left, RunTimeStamp right)
        {
            return new RunTimeStamp(left.TimeType, left.Time - right.ConvertTo(left.TimeType).Time);
        }
        public static RunTimeStamp operator -(RunTimeStamp left, float right)
        {
            return new RunTimeStamp(left.TimeType, left.Time - right);
        }

        public static RunTimeStamp operator *(RunTimeStamp left, float right)
        {
            return new RunTimeStamp(left.TimeType, left.Time * right);
        }

        public static RunTimeStamp operator /(RunTimeStamp left, float right)
        {
            return new RunTimeStamp(left.TimeType, left.Time / right);
        }

        public static implicit operator RunTimeStamp(Run.FixedTimeStamp fixedTimeStamp)
        {
            return new RunTimeStamp(RunTimerType.Realtime, fixedTimeStamp.t);
        }

        public static explicit operator Run.FixedTimeStamp(RunTimeStamp runTimeStamp)
        {
            return new Run.FixedTimeStamp(runTimeStamp.ConvertTo(RunTimerType.Realtime).Time);
        }
    }
}
