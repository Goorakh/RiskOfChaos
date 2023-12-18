using BepInEx.Configuration;
using RiskOfOptions.OptionConfigs;
using System;
using System.Collections.Generic;

namespace RiskOfChaos.ConfigHandling
{
    public class ConfigFactory<T>
    {
        readonly string _key;
        readonly T _defaultValue;

        ConfigDescription _description;
        IEqualityComparer<T> _equalityComparer;

        ValueConstrictor<T> _valueConstrictor;
        ValueValidator<T> _valueValidator;

        BaseOptionConfig _optionConfig;

        ConfigFlags _flags;

        readonly List<EventHandler<ConfigChangedArgs<T>>> _configChangedListeners = new List<EventHandler<ConfigChangedArgs<T>>>();

        readonly List<string> _previousKeys = new List<string>();

        readonly List<string> _previousSections = new List<string>();

        ConfigFactory(string key, T defaultValue)
        {
            _key = key;
            _defaultValue = defaultValue;
        }

        public static ConfigFactory<T> CreateConfig(string key, T defaultValue)
        {
            return new ConfigFactory<T>(key, defaultValue);
        }

        public ConfigFactory<T> Description(string description)
        {
            return Description(new ConfigDescription(description));
        }

        public ConfigFactory<T> Description(ConfigDescription description)
        {
            _description = description;
            return this;
        }

        public ConfigFactory<T> EqualityComparer(IEqualityComparer<T> equalityComparer)
        {
            _equalityComparer = equalityComparer;
            return this;
        }

        public ConfigFactory<T> ValueConstrictor(ValueConstrictor<T> valueConstrictor)
        {
            _valueConstrictor = valueConstrictor;
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

        public ConfigHolder<T> Build()
        {
            ConfigHolder<T> configHolder = new ConfigHolder<T>(_key,
                                                               _defaultValue,
                                                               _description ?? ConfigDescription.Empty,
                                                               _equalityComparer ?? EqualityComparer<T>.Default,
                                                               _valueConstrictor ?? CommonValueConstrictors.None<T>(),
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
