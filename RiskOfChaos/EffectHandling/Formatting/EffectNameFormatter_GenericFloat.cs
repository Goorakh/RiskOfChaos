using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling.Formatting
{
    public class EffectNameFormatter_GenericFloat : EffectNameFormatter
    {
        public float Value { get; private set; }

        public string ValueFormat = string.Empty;

        public EffectNameFormatter_GenericFloat(float value)
        {
            Value = value;
        }

        public EffectNameFormatter_GenericFloat()
        {
        }

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(Value);
            writer.Write(ValueFormat);
        }

        public override void Deserialize(NetworkReader reader)
        {
            Value = reader.ReadSingle();
            ValueFormat = reader.ReadString();
        }

        public override object[] GetFormatArgs()
        {
            return new object[] { Value.ToString(ValueFormat) };
        }

        public override bool Equals(EffectNameFormatter other)
        {
            return other is EffectNameFormatter_GenericFloat genericFloatFormatter &&
                   Value == genericFloatFormatter.Value &&
                   ValueFormat == genericFloatFormatter.ValueFormat;
        }
    }
}
