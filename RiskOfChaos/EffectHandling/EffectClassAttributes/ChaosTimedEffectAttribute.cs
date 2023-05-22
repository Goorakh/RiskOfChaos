using System;
using System.Collections.Generic;
using System.Text;

namespace RiskOfChaos.EffectHandling.EffectClassAttributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class ChaosTimedEffectAttribute : Attribute
    {
        public readonly TimedEffectType TimedType;
        public readonly float DurationSeconds;

        public bool AllowDuplicates { get; set; } = true;

        ChaosTimedEffectAttribute(TimedEffectType timedType, float durationSeconds)
        {
            TimedType = timedType;
            DurationSeconds = durationSeconds;
        }

        public ChaosTimedEffectAttribute(TimedEffectType timedType) : this(timedType, -1f)
        {
            if (timedType == TimedEffectType.FixedDuration)
            {
                throw new ArgumentException("Wrong constructor used to FixedDuration, use .ctor(float)");
            }
        }

        public ChaosTimedEffectAttribute(float duration) : this(TimedEffectType.FixedDuration, duration)
        {
        }
    }
}
