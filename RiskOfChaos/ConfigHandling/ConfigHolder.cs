using BepInEx.Configuration;
using RiskOfChaos.EffectHandling;
using RiskOfChaos.Utilities;
using RiskOfOptions;
using RiskOfOptions.OptionConfigs;
using RiskOfOptions.Options;
using System;
using System.Collections.Generic;

namespace RiskOfChaos.ConfigHandling
{
    public class ConfigHolder<T> : ConfigHolderBase
    {
        public readonly string Key;
        public readonly T DefaultValue;
        public readonly ConfigDescription Description;
        public readonly IEqualityComparer<T> EqualityComparer;
        public readonly ValueConstrictor<T> ValueConstrictor;
        public readonly ValueValidator<T> ValueValidator;

        BaseOptionConfig _optionConfig;

        readonly string[] _previousKeys;
        string[] _previousConfigSectionNames;

        ConfigFile _configFile;
        public ConfigEntry<T> Entry { get; private set; }

        public T Value
        {
            get
            {
                if (Entry != null)
                {
                    T value = Entry.Value;
                    if (ValueValidator(value))
                    {
                        return ValueConstrictor(value);
                    }
                }

                return DefaultValue;
            }
        }

        public event EventHandler<ConfigChangedArgs<T>> SettingChanged;

        public delegate void OnBindDelegate(ConfigEntry<T> entry);
        public event OnBindDelegate OnBind;

        public ConfigHolder(string key, T defaultValue, ConfigDescription description, IEqualityComparer<T> equalityComparer, ValueConstrictor<T> valueConstrictor, ValueValidator<T> valueValidator, BaseOptionConfig optionConfig, string[] previousKeys, string[] previousSections)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException($"'{nameof(key)}' cannot be null or empty.", nameof(key));

            Key = key;
            DefaultValue = defaultValue;
            Description = description ?? throw new ArgumentNullException(nameof(description));
            EqualityComparer = equalityComparer ?? throw new ArgumentNullException(nameof(equalityComparer));
            ValueConstrictor = valueConstrictor ?? throw new ArgumentNullException(nameof(valueConstrictor));
            ValueValidator = valueValidator ?? throw new ArgumentNullException(nameof(valueValidator));
            _optionConfig = optionConfig;
            _previousKeys = previousKeys ?? throw new ArgumentNullException(nameof(previousKeys));
            _previousConfigSectionNames = previousSections ?? throw new ArgumentNullException(nameof(previousSections));
        }

        ~ConfigHolder()
        {
            if (Entry != null)
            {
                Entry.SettingChanged -= Entry_SettingChanged;
            }
        }

        void Entry_SettingChanged(object sender, EventArgs e)
        {
            invokeSettingChanged();
        }

        void invokeSettingChanged()
        {
            SettingChanged?.Invoke(this, new ConfigChangedArgs<T>(this));
        }

        public void SetOptionConfig(BaseOptionConfig newConfig)
        {
            if (Entry != null)
                Log.Warning("Config already binded, setting config options will not work");

            _optionConfig = newConfig;
        }

        public override void Bind(ChaosEffectInfo ownerEffect)
        {
            if (_optionConfig != null)
            {
                ConfigHolder<bool> isEffectEnabledConfig = ownerEffect.IsEnabledConfig;
                if (isEffectEnabledConfig != null && (ConfigHolderBase)isEffectEnabledConfig != this)
                {
                    bool isEffectDisabled()
                    {
                        return !isEffectEnabledConfig.Value;
                    }

                    if (_optionConfig.checkIfDisabled == null)
                    {
                        _optionConfig.checkIfDisabled = isEffectDisabled;
                    }
                    else
                    {
                        BaseOptionConfig.IsDisabledDelegate isDisabled = _optionConfig.checkIfDisabled;
                        _optionConfig.checkIfDisabled = () => isDisabled() || isEffectDisabled();
                    }
                }
            }

            ArrayUtil.AppendRange(ref _previousConfigSectionNames, ownerEffect.PreviousConfigSectionNames);

            SettingChanged += (s, e) =>
            {
                ownerEffect.MarkNameFormatterDirty();
            };

            Bind(ownerEffect.ConfigFile, ownerEffect.ConfigSectionName, ChaosEffectCatalog.CONFIG_MOD_GUID, ChaosEffectCatalog.CONFIG_MOD_NAME);
        }

        public override void Bind(ConfigFile file, string section, string modGuid = null, string modName = null)
        {
            _configFile = file;

            if (_previousKeys != null && _previousKeys.Length > 0)
            {
                Entry = bindConfigFile(new ConfigDefinition(section, Key), _previousKeys);
            }
            else
            {
                Entry = bindConfigFile(new ConfigDefinition(section, Key));
            }

            if (Entry != null)
            {
                Entry.SettingChanged += Entry_SettingChanged;
                invokeSettingChanged();

                OnBind?.Invoke(Entry);
            }

            setupOption(modGuid, modName);
        }

        void setupOption(string modGuid, string modName)
        {
            if (_optionConfig == null)
                return;

            BaseOption option = ConfigOptionFactory.GetOption(Entry, _optionConfig);
            if (option == null)
                return;

            ModSettingsManager.AddOption(option, modGuid, modName);
        }

        ConfigEntry<T> bindConfigFile(ConfigDefinition definition, string[] previousKeys)
        {
            if (definition is null)
                throw new ArgumentNullException(nameof(definition));

            if (previousKeys is null)
                throw new ArgumentNullException(nameof(previousKeys));

            ConfigEntry<T> result = bindConfigFile(definition);

            bool foundLegacyConfig = false;
            for (int i = previousKeys.Length - 1; i >= 0; i--)
            {
                ConfigDefinition previousDefinition = new ConfigDefinition(definition.Section, previousKeys[i]);

                ConfigEntry<T> previousConfigEntry = bindConfigFile(previousDefinition);
                if (!foundLegacyConfig && !EqualityComparer.Equals(previousConfigEntry.Value, DefaultValue))
                {
                    result.Value = previousConfigEntry.Value;

#if DEBUG
                    Log.Debug($"Previous config entry found for {definition}: ({previousConfigEntry.Definition}), overriding value");
#endif

                    foundLegacyConfig = true;
                }

                _configFile.Remove(previousConfigEntry.Definition);
            }

            return result;
        }

        ConfigEntry<T> bindConfigFile(ConfigDefinition definition)
        {
            if (definition is null)
                throw new ArgumentNullException(nameof(definition));

            if (_configFile.TryGetEntry(definition, out ConfigEntry<T> existingEntry))
                return existingEntry;

            ConfigEntry<T> result = _configFile.Bind(definition, DefaultValue, Description);

            if (_previousConfigSectionNames != null)
            {
                bool foundLegacyConfig = false;
                for (int i = _previousConfigSectionNames.Length - 1; i >= 0; i--)
                {
                    // TryGetValue only works if the config is already binded, so we have to re-bind it every time to check :(
                    ConfigEntry<T> previousConfigEntry = _configFile.Bind(new ConfigDefinition(_previousConfigSectionNames[i], definition.Key), DefaultValue);
                    if (!foundLegacyConfig && !EqualityComparer.Equals(previousConfigEntry.Value, DefaultValue))
                    {
                        result.Value = previousConfigEntry.Value;

#if DEBUG
                        Log.Debug($"Previous config entry found for {definition}, overriding value");
#endif

                        foundLegacyConfig = true;
                    }

                    _configFile.Remove(previousConfigEntry.Definition);
                }
            }

            return result;
        }
    }
}
