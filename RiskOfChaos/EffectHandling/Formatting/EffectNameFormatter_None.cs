using System;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling.Formatting
{
    public sealed class EffectNameFormatter_None : EffectNameFormatter
    {
        public static readonly EffectNameFormatter_None Instance = new EffectNameFormatter_None();

        public EffectNameFormatter_None()
        {
        }

        public override void Serialize(NetworkWriter writer)
        {
        }

        public override void Deserialize(NetworkReader reader)
        {
        }

        public override object[] GetFormatArgs()
        {
            return Array.Empty<object>();
        }

        public override bool Equals(EffectNameFormatter other)
        {
            return other is EffectNameFormatter_None;
        }
    }
}
