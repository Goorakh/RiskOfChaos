using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling.Formatting
{
    public class EffectNameFormatter_PluralizedCount : EffectNameFormatter
    {
        public int Count { get; private set; }

        public string CountFormat = string.Empty;

        public string PluralString = "s";

        public EffectNameFormatter_PluralizedCount()
        {
        }

        public EffectNameFormatter_PluralizedCount(int count)
        {
            Count = count;
        }

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(Count);
            writer.Write(CountFormat);
            writer.Write(PluralString);
        }

        public override void Deserialize(NetworkReader reader)
        {
            Count = reader.ReadInt32();
            CountFormat = reader.ReadString();
            PluralString = reader.ReadString();
        }

        public override object[] GetFormatArgs()
        {
            return [
                Count.ToString(CountFormat),
                Count > 1 ? PluralString : string.Empty
            ];
        }

        public override bool Equals(EffectNameFormatter other)
        {
            return other is EffectNameFormatter_PluralizedCount nameFormatter &&
                   Count == nameFormatter.Count &&
                   CountFormat == nameFormatter.CountFormat &&
                   PluralString == nameFormatter.PluralString;
        }
    }
}
