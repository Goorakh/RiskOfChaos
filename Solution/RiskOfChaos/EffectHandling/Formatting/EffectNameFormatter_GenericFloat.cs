using RiskOfChaos.ConfigHandling;
using System;
using UnityEngine.Networking;

namespace RiskOfChaos.EffectHandling.Formatting
{
    public sealed class EffectNameFormatter_GenericFloat : EffectNameFormatter, IDisposable
    {
        readonly ConfigHolder<float> _floatValueConfig;

        float _floatValue;
        public float FloatValue
        {
            get
            {
                return _floatValue;
            }
            set
            {
                if (_floatValue == value)
                    return;

                _floatValue = value;
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

        public EffectNameFormatter_GenericFloat(float value)
        {
            FloatValue = value;
        }

        public EffectNameFormatter_GenericFloat(ConfigHolder<float> valueConfig) : this(valueConfig.Value)
        {
            _floatValueConfig = valueConfig;
            _floatValueConfig.SettingChanged += onFloatValueConfigChanged;
        }

        public EffectNameFormatter_GenericFloat()
        {
        }

        public void Dispose()
        {
            if (_floatValueConfig != null)
            {
                _floatValueConfig.SettingChanged -= onFloatValueConfigChanged;
            }
        }

        void onFloatValueConfigChanged(object sender, ConfigChangedArgs<float> e)
        {
            if (NetworkServer.active)
            {
                FloatValue = _floatValueConfig.Value;
            }
        }

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(FloatValue);
            writer.Write(ValueFormat);
        }

        public override void Deserialize(NetworkReader reader)
        {
            _floatValue = reader.ReadSingle();
            _valueFormat = reader.ReadString();

            invokeFormatterDirty();
        }

        public override object[] GetFormatArgs()
        {
            return [ FloatValue.ToString(ValueFormat) ];
        }

        public override bool Equals(EffectNameFormatter other)
        {
            return other is EffectNameFormatter_GenericFloat genericFloatFormatter &&
                   FloatValue == genericFloatFormatter.FloatValue &&
                   ValueFormat == genericFloatFormatter.ValueFormat;
        }
    }
}
