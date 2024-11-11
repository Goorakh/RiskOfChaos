using RiskOfChaos.ConfigHandling;
using System;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling.Formatting
{
    public sealed class EffectNameFormatter_PluralizedCount : EffectNameFormatter, IDisposable
    {
        readonly ConfigHolder<int> _countConfig;

        int _count;
        public int Count
        {
            get
            {
                return _count;
            }
            set
            {
                if (_count == value)
                    return;

                _count = value;
                invokeFormatterDirty();
            }
        }

        string _countFormat = string.Empty;
        public string CountFormat
        {
            get
            {
                return _countFormat;
            }
            set
            {
                if (_countFormat == value)
                    return;

                _countFormat = value;
                invokeFormatterDirty();
            }
        }

        string _pluralString = "s";
        public string PluralString
        {
            get
            {
                return _pluralString;
            }
            set
            {
                if (_pluralString == value)
                    return;

                _pluralString = value;
                invokeFormatterDirty();
            }
        }

        public EffectNameFormatter_PluralizedCount(int count)
        {
            Count = count;
        }

        public EffectNameFormatter_PluralizedCount(ConfigHolder<int> countConfig) : this(countConfig.Value)
        {
            _countConfig = countConfig;
            _countConfig.SettingChanged += onCountConfigChanged;
        }

        public EffectNameFormatter_PluralizedCount()
        {
        }

        public void Dispose()
        {
            if (_countConfig != null)
            {
                _countConfig.SettingChanged -= onCountConfigChanged;
            }
        }

        void onCountConfigChanged(object sender, ConfigChangedArgs<int> e)
        {
            if (NetworkServer.active)
            {
                Count = _countConfig.Value;
            }
        }

        public override void Serialize(NetworkWriter writer)
        {
            writer.WritePackedUInt32((uint)Count);
            writer.Write(CountFormat);
            writer.Write(PluralString);
        }

        public override void Deserialize(NetworkReader reader)
        {
            _count = (int)reader.ReadPackedUInt32();
            _countFormat = reader.ReadString();
            PluralString = reader.ReadString();

            invokeFormatterDirty();
        }

        public override object[] GetFormatArgs()
        {
            return [
                Count.ToString(CountFormat),
                Count != 1 ? PluralString : string.Empty
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
