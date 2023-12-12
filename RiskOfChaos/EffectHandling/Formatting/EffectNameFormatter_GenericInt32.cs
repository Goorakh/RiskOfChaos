using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling.Formatting
{
    public class EffectNameFormatter_GenericInt32 : EffectNameFormatter
    {
        public int Value { get; private set; }

        public string ValueFormat = string.Empty;

        public EffectNameFormatter_GenericInt32()
        {
        }

        public EffectNameFormatter_GenericInt32(int value)
        {
            Value = value;
        }

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(Value);
            writer.Write(ValueFormat);
        }

        public override void Deserialize(NetworkReader reader)
        {
            Value = reader.ReadInt32();
            ValueFormat = reader.ReadString();
        }

        public override object[] GetFormatArgs()
        {
            return new object[] { Value.ToString(ValueFormat) };
        }

        public override bool Equals(EffectNameFormatter other)
        {
            return other is EffectNameFormatter_GenericInt32 formatter &&
                   Value == formatter.Value &&
                   ValueFormat == formatter.ValueFormat;
        }
    }
}
