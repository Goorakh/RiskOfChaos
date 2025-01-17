using BepInEx.Configuration;
using RiskOfChaos.EffectHandling;
using RiskOfOptions.OptionConfigs;
using System;

namespace RiskOfChaos.ConfigHandling
{
    public abstract class ConfigHolderBase
    {
        public readonly string Key;
        public readonly ConfigDescription Description;
        public readonly ConfigFlags Flags;

        protected BaseOptionConfig _optionConfig;

        protected readonly string[] _previousKeys;
        protected string[] _previousConfigSectionNames;

        protected ConfigFile _configFile;

        public ConfigDefinition Definition { get; protected set; }

        public ConfigEntryBase Entry { get; protected set; }

        protected bool _hasServerOverrideValue = false;
        protected object _serverOverrideValue;

        public object LocalBoxedValue
        {
            get => Entry.BoxedValue;
            set => Entry.BoxedValue = value;
        }

        public object BoxedValue => _hasServerOverrideValue ? _serverOverrideValue : LocalBoxedValue;

        public abstract bool IsDefaultValue { get; }

        public event EventHandler<ConfigChangedArgs> SettingChanged;

        public delegate void OnBindDelegate(ConfigEntryBase entry);
        public event OnBindDelegate OnBind;

        protected ConfigHolderBase(string key, ConfigDescription description, string[] previousKeys, string[] previousSections, ConfigFlags flags)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException($"'{nameof(key)}' cannot be null or empty.", nameof(key));

            Key = key;
            Description = description ?? throw new ArgumentNullException(nameof(description));
            _previousKeys = previousKeys ?? throw new ArgumentNullException(nameof(previousKeys));
            _previousConfigSectionNames = previousSections ?? throw new ArgumentNullException(nameof(previousSections));
            Flags = flags;
        }

        public abstract void Bind(ChaosEffectInfo effectInfo);

        public abstract void Bind(ConfigFile file, string section, string modGuid, string modName);

        public void SetOptionConfig(BaseOptionConfig newConfig)
        {
            if (Entry != null)
                Log.Warning("Config already binded, setting config options will not work");

            _optionConfig = newConfig;
        }

        protected virtual void invokeSettingChanged()
        {
            try
            {
                SettingChanged?.Invoke(this, new ConfigChangedArgs(this));
            }
            catch (Exception e)
            {
                Log.Error_NoCallerPrefix($"SettingChanged invoke failed for {Definition}: {e}");
            }
        }

        protected virtual void invokeOnBind()
        {
            try
            {
                OnBind?.Invoke(Entry);
            }
            catch (Exception e)
            {
                Log.Error_NoCallerPrefix($"OnBind invoke failed for {Definition}: {e}");
            }
        }

        public void SetServerOverrideValue(object value)
        {
            _serverOverrideValue = value;
            _hasServerOverrideValue = true;

            invokeSettingChanged();
        }

        public void ClearServerOverrideValue()
        {
            if (_hasServerOverrideValue)
            {
                _serverOverrideValue = null;
                _hasServerOverrideValue = false;

                invokeSettingChanged();
            }
        }
    }
}
