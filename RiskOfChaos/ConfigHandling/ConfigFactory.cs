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

        BaseOptionConfig _optionConfig;

        readonly List<EventHandler<ConfigChangedArgs<T>>> _configChangedListeners = new List<EventHandler<ConfigChangedArgs<T>>>();

        readonly List<string> _previousKeys = new List<string>();

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

        public ConfigHolder<T> Build()
        {
            ConfigHolder<T> configHolder = new ConfigHolder<T>(_key,
                                                               _defaultValue,
                                                               _description ?? ConfigDescription.Empty,
                                                               _equalityComparer ?? EqualityComparer<T>.Default,
                                                               _valueConstrictor ?? ValueConstrictors.None<T>(),
                                                               _optionConfig,
                                                               _previousKeys.ToArray());

            foreach (EventHandler<ConfigChangedArgs<T>> listener in _configChangedListeners)
            {
                configHolder.SettingChanged += listener;
            }

            return configHolder;
        }
    }
}
