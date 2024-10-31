using BepInEx.Configuration;
using RiskOfChaos.ConfigHandling;
using System;
using UnityEngine.Networking;

namespace RiskOfChaos.Networking.Components
{
    public class SyncConfigValue : NetworkBehaviour
    {
        [SyncVar]
        string _configDefinition;

        [SyncVar(hook = nameof(syncSerializedConfigValue))]
        string _serializedConfigValue;

        ConfigHolderBase _configHolder;
        public ConfigHolderBase Config
        {
            get
            {
                if (NetworkServer.active)
                {
                    return _configHolder;
                }
                else
                {
                    return NetworkedConfigManager.TryGetConfigByDefinition(_configDefinition, out ConfigHolderBase configHolder) ? configHolder : null;
                }
            }
            [Server]
            set
            {
                if (_configHolder is not null)
                {
                    _configHolder.SettingChanged -= onSettingChanged;
                    _configHolder.OnBind -= onConfigBind;
                }

                _configHolder = value;

                _configDefinition = null;
                if (_configHolder is not null)
                {
                    _configHolder.SettingChanged += onSettingChanged;

                    if (_configHolder.Entry is null)
                    {
                        _configHolder.OnBind += onConfigBind;
                    }
                    else
                    {
                        _configDefinition = _configHolder.Definition.ToString();
                        setValue(_configHolder.LocalBoxedValue, _configHolder.Entry.SettingType);
                    }
                }
            }
        }

        void onConfigBind(ConfigEntryBase entry)
        {
            _configDefinition = entry.Definition.ToString();
            setValue(entry.BoxedValue, entry.SettingType);
        }

        void onSettingChanged(object sender, ConfigChangedArgs e)
        {
            setValue(e.NewValue, e.Holder.Entry.SettingType);
        }

        void setValue(object value, Type settingType)
        {
            _serializedConfigValue = TomlTypeConverter.ConvertToString(value, settingType);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            syncSerializedConfigValue(_serializedConfigValue);
        }

        void OnDestroy()
        {
            NetworkedConfigManager.ClearOverrideValue(_configDefinition);
            
            if (_configHolder != null)
            {
                _configHolder.SettingChanged -= onSettingChanged;
                _configHolder.OnBind -= onConfigBind;
            }
        }

        void syncSerializedConfigValue(string serializedConfigValue)
        {
            _serializedConfigValue = serializedConfigValue;

            if (!NetworkServer.active)
            {
                NetworkedConfigManager.SetOverrideValue(_configDefinition, serializedConfigValue);
            }
        }
    }
}
