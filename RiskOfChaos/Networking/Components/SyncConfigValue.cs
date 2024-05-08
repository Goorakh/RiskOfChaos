using BepInEx.Configuration;
using RiskOfChaos.ConfigHandling;
using System;
using UnityEngine.Networking;

namespace RiskOfChaos.Networking.Components
{
    public class SyncConfigValue : NetworkBehaviour
    {
        string _configDefinition;
        const uint CONFIG_DEFINITION_DIRTY_BIT = 1 << 0;

        string configDefinition
        {
            get
            {
                return _configDefinition;
            }
            set
            {
                SetSyncVar(value, ref _configDefinition, CONFIG_DEFINITION_DIRTY_BIT);
            }
        }

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
            set
            {
                if (!NetworkServer.active)
                {
                    Log.Error("Cannot set config as client");
                    return;
                }

                if (_configHolder is not null)
                {
                    _configHolder.SettingChanged -= onSettingChanged;
                    _configHolder.OnBind -= onConfigBind;
                }

                _configHolder = value;

                configDefinition = null;
                if (_configHolder is not null)
                {
                    _configHolder.SettingChanged += onSettingChanged;

                    if (_configHolder.Entry is null)
                    {
                        _configHolder.OnBind += onConfigBind;
                    }
                    else
                    {
                        configDefinition = _configHolder.Definition.ToString();
                        setValue(_configHolder.LocalBoxedValue, _configHolder.Entry.SettingType);
                    }
                }
            }
        }

        void onConfigBind(ConfigEntryBase entry)
        {
            configDefinition = entry.Definition.ToString();
            setValue(entry.BoxedValue, entry.SettingType);
        }

        void onSettingChanged(object sender, ConfigChangedArgs e)
        {
            setValue(e.NewValue, e.Holder.Entry.SettingType);
        }

        void setValue(object value, Type settingType)
        {
            NetworkSerializedConfigValue = TomlTypeConverter.ConvertToString(value, settingType);
        }

        string _serializedConfigValue;
        const uint SERIALIZED_CONFIG_VALUE_DIRTY_BIT = 1 << 1;

        public string NetworkSerializedConfigValue
        {
            get
            {
                return _serializedConfigValue;
            }
            set
            {
                if (NetworkServer.localClientActive && !syncVarHookGuard)
                {
                    syncVarHookGuard = true;
                    syncSerializedConfigValue(value);
                    syncVarHookGuard = false;
                }

                SetSyncVar(value, ref _serializedConfigValue, SERIALIZED_CONFIG_VALUE_DIRTY_BIT);
            }
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            syncSerializedConfigValue(_serializedConfigValue);
        }

        void syncSerializedConfigValue(string serializedConfigValue)
        {
            NetworkSerializedConfigValue = serializedConfigValue;

            if (!NetworkServer.active)
            {
                NetworkedConfigManager.SetOverrideValue(configDefinition, serializedConfigValue);
            }
        }

        void OnDestroy()
        {
            NetworkedConfigManager.ClearOverrideValue(configDefinition);
        }

        public override bool OnSerialize(NetworkWriter writer, bool initialState)
        {
            if (initialState)
            {
                writer.Write(configDefinition);
                writer.Write(_serializedConfigValue);

                return true;
            }

            uint dirtyBits = syncVarDirtyBits;
            writer.WritePackedUInt32(dirtyBits);

            bool anythingWritten = false;

            if ((dirtyBits & CONFIG_DEFINITION_DIRTY_BIT) != 0)
            {
                writer.Write(configDefinition);
                anythingWritten = true;
            }

            if ((dirtyBits & SERIALIZED_CONFIG_VALUE_DIRTY_BIT) != 0)
            {
                writer.Write(_serializedConfigValue);
                anythingWritten = true;
            }

            return anythingWritten;
        }

        public override void OnDeserialize(NetworkReader reader, bool initialState)
        {
            if (initialState)
            {
                _configDefinition = reader.ReadString();
                _serializedConfigValue = reader.ReadString();

                return;
            }

            uint dirtyBits = reader.ReadPackedUInt32();

            if ((dirtyBits & CONFIG_DEFINITION_DIRTY_BIT) != 0)
            {
                _configDefinition = reader.ReadString();
            }

            if ((dirtyBits & SERIALIZED_CONFIG_VALUE_DIRTY_BIT) != 0)
            {
                syncSerializedConfigValue(reader.ReadString());
            }
        }
    }
}
