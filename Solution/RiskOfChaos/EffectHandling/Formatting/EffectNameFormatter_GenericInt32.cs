using RiskOfChaos.ConfigHandling;
using System;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling.Formatting
{
    public sealed class EffectNameFormatter_GenericInt32 : EffectNameFormatter, IDisposable
    {
        readonly ConfigHolder<int> _valueConfig;

        int _value;
        public int Value
        {
            get
            {
                return _value;
            }
            set
            {
                if (_value == value)
                    return;

                _value = value;
                invokeFormatterDirty();
            }
        }

        string _valueFormat = string.Empty;
        public string ValueFormat
        {
            get
            {
                return _valueFormat;
            }
            set
            {
                if (_valueFormat == value)
                    return;

                _valueFormat = value;
                invokeFormatterDirty();
            }
        }

        public EffectNameFormatter_GenericInt32()
        {
        }

        public EffectNameFormatter_GenericInt32(int value)
        {
            Value = value;
        }

        public EffectNameFormatter_GenericInt32(ConfigHolder<int> valueConfig) : this(valueConfig.Value)
        {
            _valueConfig = valueConfig;
            _valueConfig.SettingChanged += onValueConfigChanged;
        }

        public void Dispose()
        {
            if (_valueConfig != null)
            {
                _valueConfig.SettingChanged -= onValueConfigChanged;
            }
        }

        void onValueConfigChanged(object sender, ConfigChangedArgs<int> e)
        {
            if (NetworkServer.active)
            {
                Value = _valueConfig.Value;
            }
        }

        public override void Serialize(NetworkWriter writer)
        {
            writer.WritePackedUInt32((uint)Value);
            writer.Write(ValueFormat);
        }

        public override void Deserialize(NetworkReader reader)
        {
            _value = (int)reader.ReadPackedUInt32();
            _valueFormat = reader.ReadString();

            invokeFormatterDirty();
        }

        public override object[] GetFormatArgs()
        {
            return [ Value.ToString(ValueFormat) ];
        }

        public override bool Equals(EffectNameFormatter other)
        {
            return other is EffectNameFormatter_GenericInt32 formatter &&
                   Value == formatter.Value &&
                   ValueFormat == formatter.ValueFormat;
        }
    }
}
