using BepInEx.Configuration;
using RiskOfChaos.EffectDefinitions;
using System;

namespace RiskOfChaos.EffectHandling.EffectClassAttributes
{
    public sealed class ChaosTimedEffectAttribute : ChaosEffectAttribute
    {
        public readonly TimedEffectType TimedType;
        public readonly float DurationSeconds;

        public bool AllowDuplicates { get; set; } = true;

        public bool HideFromEffectsListWhenPermanent { get; set; } = false;

        public ushort DefaultStageCountDuration { get; set; } = 1;

        public bool IgnoreDurationModifiers { get; set; } = false;

        ChaosTimedEffectAttribute(string identifier, TimedEffectType timedType, float durationSeconds) : base(identifier)
        {
            TimedType = timedType;
            DurationSeconds = durationSeconds;
        }

        public ChaosTimedEffectAttribute(string identifier, TimedEffectType timedType) : this(identifier, timedType, -1f)
        {
            if (timedType == TimedEffectType.FixedDuration)
            {
                throw new ArgumentException("Wrong constructor used to FixedDuration, use .ctor(float)");
            }
        }

        public ChaosTimedEffectAttribute(string identifier, float duration) : this(identifier, TimedEffectType.FixedDuration, duration)
        {
        }

        public override ChaosEffectInfo BuildEffectInfo(ChaosEffectIndex index, ConfigFile configFile)
        {
            return new TimedEffectInfo(index, this, configFile);
        }
    }
}
