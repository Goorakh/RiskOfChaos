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

        IEqualityComparer<T> _equalityComparer = EqualityComparer<T>.Default;

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
            if (equalityComparer is null)
                throw new ArgumentNullException(nameof(equalityComparer));

            _equalityComparer = equalityComparer;
            return this;
        }

        public ConfigFactory<T> OptionConfig(BaseOptionConfig optionConfig)
        {
            _optionConfig = optionConfig;
            return this;
        }

        public ConfigFactory<T> OnValueChanged(EventHandler<ConfigChangedArgs<T>> listener)
        {
            if (listener is null)
                throw new ArgumentNullException(nameof(listener));

            _configChangedListeners.Add(listener);
            return this;
        }

        public ConfigFactory<T> OnValueChanged(Action listener)
        {
            if (listener is null)
                throw new ArgumentNullException(nameof(listener));

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

        public ConfigHolder<T> Build()
        {
            ConfigDescription description = ConfigDescription.Empty;
            if (_customDescriptionInstance != null)
            {
                description = _customDescriptionInstance;
            }
            else if (!string.IsNullOrEmpty(_descriptionText) || _acceptableValues != null)
            {
                string descriptionText = string.Empty;
                if (!string.IsNullOrEmpty(_descriptionText))
                {
                    descriptionText = _descriptionText;
                }

                description = new ConfigDescription(descriptionText, _acceptableValues);
            }

            ConfigHolder<T> configHolder = new ConfigHolder<T>(_key,
                                                               _defaultValue,
                                                               description,
                                                               _equalityComparer,
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
