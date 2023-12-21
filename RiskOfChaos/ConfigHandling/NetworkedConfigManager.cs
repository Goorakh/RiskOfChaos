using BepInEx.Configuration;
using RiskOfChaos.Networking.Components;
using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskOfChaos.ConfigHandling
{
    public static class NetworkedConfigManager
    {
        static bool _hasRegisteredRunStartEvent = false;

        static readonly Dictionary<string, ConfigHolderBase> _networkedConfigHolders = new Dictionary<string, ConfigHolderBase>();

        public static void RegisterNetworkedConfig(ConfigHolderBase configHolder)
        {
            string key = configHolder.Definition.ToString();
            _networkedConfigHolders.Add(key, configHolder);

#if DEBUG
            Log.Debug($"Registered networked config: '{key}'");
#endif

            if (!_hasRegisteredRunStartEvent)
            {
                Run.onRunStartGlobal += _ =>
                {
                    if (!NetworkServer.active)
                        return;

                    foreach (ConfigHolderBase networkedConfigHolder in _networkedConfigHolders.Values)
                    {
                        GameObject configNetworker = GameObject.Instantiate(NetPrefabs.ConfigNetworkerPrefab);

                        SyncConfigValue syncConfigValue = configNetworker.GetComponent<SyncConfigValue>();
                        syncConfigValue.Config = networkedConfigHolder;

                        NetworkServer.Spawn(configNetworker);
                    }
                };

                _hasRegisteredRunStartEvent = true;
            }
        }

        public static bool TryGetConfigByDefinition(string definition, out ConfigHolderBase configHolder)
        {
            if (string.IsNullOrEmpty(definition))
            {
                configHolder = null;
                return false;
            }

            return _networkedConfigHolders.TryGetValue(definition, out configHolder);
        }

        public static void SetOverrideValue(string definition, string serializedValue)
        {
            if (!TryGetConfigByDefinition(definition, out ConfigHolderBase configHolder))
            {
                Log.Error($"Networked config '{definition}' is not defined");
                return;
            }

            if (string.IsNullOrEmpty(serializedValue))
            {
                configHolder.ClearServerOverrideValue();

#if DEBUG
                Log.Debug($"Cleared server override value for '{definition}'");
#endif
            }
            else
            {
                if (configHolder.Entry is null)
                {
                    Log.Error($"Config '{definition}' has not been binded");
                    return;
                }

                object value;
                try
                {
                    value = TomlTypeConverter.ConvertToValue(serializedValue, configHolder.Entry.SettingType);
                }
                catch (Exception e)
                {
                    Log.Error_NoCallerPrefix($"Failed to deserialize networked config value for '{definition}': {e}");
                    return;
                }

                configHolder.SetServerOverrideValue(value);

#if DEBUG
                Log.Debug($"Set server override value: '{definition}' = {value}");
#endif
            }
        }

        public static void ClearOverrideValue(string definition)
        {
            if (!TryGetConfigByDefinition(definition, out ConfigHolderBase configHolder))
            {
                Log.Error($"Networked config '{definition}' is not defined");
                return;
            }

            configHolder.ClearServerOverrideValue();

#if DEBUG
            Log.Debug($"Cleared server override value for '{definition}'");
#endif
        }
    }
}
