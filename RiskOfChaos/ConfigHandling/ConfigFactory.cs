using BepInEx.Configuration;
using RiskOfChaos.Utilities.Extensions;
using RiskOfOptions.OptionConfigs;
using System;
using System.Collections.Generic;

namespace RiskOfChaos.ConfigHandling
{
    public class ConfigFactory<T>
    {
        readonly string _key;
        readonly T _defaultValue;

        ConfigDescription _customDescriptionInstance;

        string _descriptionText;
        AcceptableValueBase _acceptableValues;

        IEqualityComparer<T> _equalityComparer;

        ValueValidator<T> _valueValidator;

        BaseOptionConfig _optionConfig;

        ConfigFlags _flags;

        readonly List<EventHandler<ConfigChangedArgs<T>>> _configChangedListeners = [];

        readonly List<string> _previousKeys = [];

        readonly List<string> _previousSections = [];

        ConfigFactory(string key, T defaultValue)
        {
            _key = key;
            _defaultValue = defaultValue;
        }

        public static ConfigFactory<T> CreateConfig(string key, T defaultValue)
        {
            return new ConfigFactory<T>(key.FilterConfigKey(), defaultValue);
        }

        public ConfigFactory<T> Description(string description)
        {
            _descriptionText = description;
            return this;
        }

        public ConfigFactory<T> AcceptableValues(AcceptableValueBase acceptableValues)
        {
            _acceptableValues = acceptableValues;
            return this;
        }

        public ConfigFactory<T> Description(ConfigDescription description)
        {
            _customDescriptionInstance = description;
            return this;
        }

        public ConfigFactory<T> EqualityComparer(IEqualityComparer<T> equalityComparer)
        {
            _equalityComparer = equalityComparer;
            return this;
        }

        public ConfigFactory<T> ValueValidator(ValueValidator<T> valueValidator)
        {
            _valueValidator = valueValidator;
            return this;
        }

        public ConfigFactory<T> OptionConfig(BaseOptionConfig optionConfig)
        {
            _optionConfig = optionConfig;
            return this;
        }

        public ConfigFactory<T> OnValueChanged(EventHandler<ConfigChangedArgs<T>> listener)
        {
            _configChangedListeners.Add(listener);
            return this;
        }

        public ConfigFactory<T> OnValueChanged(Action listener)
        {
            return OnValueChanged((s, e) => listener());
        }

        public ConfigFactory<T> RenamedFrom(string key)
        {
            _previousKeys.Add(key);
            return this;
        }

        public ConfigFactory<T> MovedFrom(string section)
        {
            _previousSections.Add(section);
            return this;
        }

        public ConfigFactory<T> Networked()
        {
            _flags |= ConfigFlags.Networked;
            return this;
        }

        public ConfigFactory<T> FormatsEffectName()
        {
            _flags |= ConfigFlags.FormatsEffectName;
            return this;
        }

        public ConfigHolder<T> Build()
        {
            ConfigDescription description;
            if (_customDescriptionInstance != null)
            {
                description = _customDescriptionInstance;
            }
            else if (!string.IsNullOrEmpty(_descriptionText) || _acceptableValues != null)
            {
                description = new ConfigDescription(_descriptionText ?? string.Empty, _acceptableValues);
            }
            else
            {
                description = ConfigDescription.Empty;
            }

            ConfigHolder<T> configHolder = new ConfigHolder<T>(_key,
                                                               _defaultValue,
                                                               description,
                                                               _equalityComparer ?? EqualityComparer<T>.Default,
                                                               _valueValidator ?? CommonValueValidators.None<T>(),
                                                               _optionConfig,
                                                               _previousKeys.ToArray(),
                                                               _previousSections.ToArray(),
                                                               _flags);

            foreach (EventHandler<ConfigChangedArgs<T>> listener in _configChangedListeners)
            {
                configHolder.SettingChanged += listener;
            }

            return configHolder;
        }
    }
}
