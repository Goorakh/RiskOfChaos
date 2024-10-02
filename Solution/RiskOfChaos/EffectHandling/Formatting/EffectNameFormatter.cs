using System;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling.Formatting
{
    public abstract class EffectNameFormatter : IEquatable<EffectNameFormatter>
    {
        public EffectNameFormatter()
        {
        }

        public abstract void Serialize(NetworkWriter writer);

        public abstract void Deserialize(NetworkReader reader);

        public abstract object[] GetFormatArgs();

        public virtual string FormatEffectName(string effectName)
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
            if (left is null || right is null)
                return left is null && right is null;

            return left.Equals(right);
        }

        public static bool operator !=(EffectNameFormatter left, EffectNameFormatter right)
        {
            return !(left == right);
        }
    }
}
