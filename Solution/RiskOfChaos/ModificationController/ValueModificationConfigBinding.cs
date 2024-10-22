using MonoMod.Utils;
using RiskOfChaos.ConfigHandling;
using System;

namespace RiskOfChaos.ModificationController
{
    public sealed class ValueModificationConfigBinding<T> : IDisposable
    {
        static readonly bool _isTypeNullable = typeof(T).IsClass || (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(Nullable<>));

        public delegate void ValueSetterDelegate(T value);
        public delegate T ValueConverterDelegate(object inValue);

        readonly ValueSetterDelegate _valueSetterFunc;

        ConfigHolderBase _boundToConfig;
        public ConfigHolderBase BoundToConfig
        {
            get
            {
                return _boundToConfig;
            }
            private set
            {
                if (_boundToConfig == value)
                    return;

                if (_boundToConfig != null)
                {
                    _boundToConfig.SettingChanged -= onBoundConfigChanged;
                }

                _boundToConfig = value;

                if (_boundToConfig != null)
                {
                    _boundToConfig.SettingChanged += onBoundConfigChanged;
                    refreshValue();
                }
            }
        }

        ValueConverterDelegate _configValueConverter;

        bool _isDisposed;

        public ValueModificationConfigBinding(ValueSetterDelegate valueSetter)
        {
            _valueSetterFunc = valueSetter ?? throw new ArgumentNullException(nameof(valueSetter));
        }
        
        ~ValueModificationConfigBinding()
        {
            dispose();
        }

        public void Dispose()
        {
            dispose();
            GC.SuppressFinalize(this);
        }

        void dispose()
        {
            if (!_isDisposed)
            {
                try
                {
                    UnbindFromConfig();
                }
                finally
                {
                    _isDisposed = true;
                }
            }
        }

        public void BindToConfig(ConfigHolder<T> configHolder, Func<T, T> valueConverter = null)
        {
            if (_isDisposed)
                return;

            _configValueConverter = valueConverter.CastDelegate<ValueConverterDelegate>();

            BoundToConfig = configHolder;
        }

        public void BindToConfigConverted<TConfig>(ConfigHolder<TConfig> configHolder, Func<TConfig, T> converter)
        {
            if (_isDisposed)
                return;

            _configValueConverter = converter.CastDelegate<ValueConverterDelegate>();

            BoundToConfig = configHolder;
        }

        public void UnbindFromConfig()
        {
            BindToConfig(null, null);
        }

        void onBoundConfigChanged(object sender, ConfigChangedArgs e)
        {
            refreshValue();
        }

        void refreshValue()
        {
            object value = BoundToConfig.BoxedValue;

            T convertedValue;
            if (_configValueConverter != null)
            {
                convertedValue = _configValueConverter(value);
            }
            else if (value is T || (value is null && _isTypeNullable))
            {
                convertedValue = (T)value;
            }
            else
            {
                Log.Error($"Cannot convert config value {BoundToConfig.Entry.SettingType} to {typeof(T)}");
                return;
            }

            _valueSetterFunc(convertedValue);
        }
    }
}
