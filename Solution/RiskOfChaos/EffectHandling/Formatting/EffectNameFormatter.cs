using RoR2;
using System;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling.Formatting
{
    public abstract class EffectNameFormatter : IEquatable<EffectNameFormatter>
    {
        public event Action OnFormatterDirty;

        public abstract void Serialize(NetworkWriter writer);

        public abstract void Deserialize(NetworkReader reader);

        public abstract object[] GetFormatArgs();

        public string FormatEffectName(string effectName)
        {
            object[] args = GetFormatArgs();
            if (args.Length == 0)
                return effectName;

            try
            {
                return string.Format(effectName, args);
            }
            catch (FormatException e)
            {
                Log.Error_NoCallerPrefix($"Caught exception formatting effect name: {e}");
                return effectName;
            }
        }

        public string GetEffectDisplayName(ChaosEffectInfo effectInfo, EffectNameFormatFlags formatFlags = EffectNameFormatFlags.All)
        {
            if (effectInfo == null)
            {
                return "???";
            }

            string displayName = Language.GetString(effectInfo.NameToken);

            if ((formatFlags & EffectNameFormatFlags.RuntimeFormatArgs) != 0)
            {
                displayName = FormatEffectName(displayName);
            }

            if ((formatFlags & EffectNameFormatFlags.TimedType) != 0)
            {
                if (effectInfo is TimedEffectInfo timedEffectInfo)
                {
                    displayName = FormatEffectTimedType(displayName, timedEffectInfo.TimedType, timedEffectInfo.Duration);
                }
            }

            return displayName;
        }

        public string FormatEffectTimedType(string effectName, TimedEffectType timedType, float duration)
        {
            switch (timedType)
            {
                case TimedEffectType.UntilStageEnd:
                    int stageCount = Mathf.CeilToInt(duration);
                    string token = stageCount == 1 ? "TIMED_TYPE_UNTIL_STAGE_END_SINGLE_FORMAT" : "TIMED_TYPE_UNTIL_STAGE_END_MULTI_FORMAT";
                    return Language.GetStringFormatted(token, effectName, stageCount);
                case TimedEffectType.FixedDuration:
                    return Language.GetStringFormatted("TIMED_TYPE_FIXED_DURATION_FORMAT", effectName, duration);
                case TimedEffectType.Permanent:
                case TimedEffectType.AlwaysActive:
                    return Language.GetStringFormatted("TIMED_TYPE_PERMANENT_FORMAT", effectName);
                default:
                    throw new NotImplementedException($"Timed type {timedType} is not implemented");
            }
        }

        public virtual string GetEffectNameSubtitle(ChaosEffectInfo effectInfo)
        {
            return string.Empty;
        }

        protected void invokeFormatterDirty()
        {
            OnFormatterDirty?.Invoke();
        }

        public abstract bool Equals(EffectNameFormatter other);

        public override bool Equals(object obj)
        {
            return obj is EffectNameFormatter other && Equals(other);
        }

        public override int GetHashCode()
        {
            return EffectNameFormatterCatalog.GetFormatterTypeIndex(this);
        }

        public static bool operator ==(EffectNameFormatter left, EffectNameFormatter right)
        {
            if (left is null)
                return right is null;

            return left.Equals(right);
        }

        public static bool operator !=(EffectNameFormatter left, EffectNameFormatter right)
        {
            return !(left == right);
        }
    }
}
