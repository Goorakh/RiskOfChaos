using RiskOfChaos.ConfigHandling;
using System;

namespace RiskOfChaos.ModificationController
{
    public sealed class ValueModificationConfigBinding<T> : IDisposable
    {
        public delegate void ValueSetterDelegate(T value);
        public delegate T ValueConverterDelegate(T inValue);

        readonly ValueSetterDelegate _valueSetterFunc;

        ConfigHolder<T> _boundToConfig;
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

        public void BindToConfig(ConfigHolder<T> configHolder, ValueConverterDelegate valueConverter = null)
        {
            if (_isDisposed)
                return;

            _configValueConverter = valueConverter;

            if (_boundToConfig != configHolder)
            {
                if (_boundToConfig != null)
                {
                    _boundToConfig.SettingChanged -= onBoundConfigChanged;
                }

                _boundToConfig = configHolder;

                if (_boundToConfig != null)
                {
                    _boundToConfig.SettingChanged += onBoundConfigChanged;
                    refreshValue();
                }
            }
        }

        public void UnbindFromConfig()
        {
            BindToConfig(null);
        }

        void onBoundConfigChanged(object sender, ConfigChangedArgs<T> e)
        {
            refreshValue();
        }

        void refreshValue()
        {
            T value = _boundToConfig.Value;

            if (_configValueConverter != null)
            {
                value = _configValueConverter(value);
            }

            _valueSetterFunc(value);
        }
    }
}
