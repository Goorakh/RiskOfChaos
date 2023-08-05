using BepInEx.Configuration;
using ProBuilder.Core;
using RiskOfChaos.EffectHandling;
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

        readonly BaseOptionConfig _optionConfig;

        readonly string[] _previousKeys;

        public ConfigEntry<T> Entry { get; private set; }

        public T Value => Entry != null ? ValueConstrictor(Entry.Value) : DefaultValue;

        public event EventHandler<ConfigChangedArgs<T>> SettingChanged;

        public ConfigHolder(string key, T defaultValue, ConfigDescription description, IEqualityComparer<T> equalityComparer, ValueConstrictor<T> valueConstrictor, BaseOptionConfig optionConfig, string[] previousKeys)
        {
            Key = key;
            DefaultValue = defaultValue;
            Description = description;
            EqualityComparer = equalityComparer;
            ValueConstrictor = valueConstrictor;
            _optionConfig = optionConfig;
            _previousKeys = previousKeys;
        }

        ~ConfigHolder()
        {
            if (Entry != null)
            {
                Entry.SettingChanged -= Entry_SettingChanged;
            }
        }

        public override void Bind(ChaosEffectInfo ownerEffect)
        {
            if (_previousKeys != null && _previousKeys.Length > 0)
            {
                Entry = ownerEffect.BindConfig(Key, _previousKeys, DefaultValue, Description, EqualityComparer);
            }
            else
            {
                Entry = ownerEffect.BindConfig(Key, DefaultValue, Description, EqualityComparer);
            }

            setupSettingChangedListener();

            if (_optionConfig != null)
            {
                ConfigEntry<bool> isEffectEnabledConfig = ownerEffect.IsEnabledConfig;
                if (isEffectEnabledConfig != null)
                {
                    bool isEffectDisabled()
                    {
                        return isEffectEnabledConfig != null && !isEffectEnabledConfig.Value;
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

            setupOption(ChaosEffectCatalog.CONFIG_MOD_GUID, ChaosEffectCatalog.CONFIG_MOD_NAME);
        }

        public override void Bind(ConfigFile file, string section, string modGuid = null, string modName = null)
        {
            if (_previousKeys.Length > 0)
            {
                Log.Warning("Previous key names not supported");
            }

            Entry = file.Bind(new ConfigDefinition(section, Key), DefaultValue, Description);

            setupSettingChangedListener();

            setupOption(modGuid, modName);
        }

        void setupSettingChangedListener()
        {
            if (Entry != null)
            {
                Entry.SettingChanged += Entry_SettingChanged;
                invokeSettingChanged();
            }
        }

        void Entry_SettingChanged(object sender, EventArgs e)
        {
            invokeSettingChanged();
        }

        private void invokeSettingChanged()
        {
            SettingChanged?.Invoke(this, new ConfigChangedArgs<T>(this));
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
    }
}
