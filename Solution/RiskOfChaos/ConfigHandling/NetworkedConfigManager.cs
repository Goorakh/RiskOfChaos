﻿using BepInEx.Configuration;
using RiskOfChaos.Content;
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
        static readonly Dictionary<string, ConfigHolderBase> _networkedConfigHolders = [];

        static void onRunStartGlobal(Run run)
        {
            if (!NetworkServer.active)
                return;

            foreach (ConfigHolderBase networkedConfigHolder in _networkedConfigHolders.Values)
            {
                GameObject configNetworker = GameObject.Instantiate(RoCContent.NetworkedPrefabs.ConfigNetworker);

                SyncConfigValue syncConfigValue = configNetworker.GetComponent<SyncConfigValue>();
                syncConfigValue.Config = networkedConfigHolder;

                NetworkServer.Spawn(configNetworker);
            }
        }

        public static void RegisterNetworkedConfig(ConfigHolderBase configHolder)
        {
            string key = configHolder.Definition.ToString();
            if (_networkedConfigHolders.ContainsKey(key))
            {
                Log.Error($"Networked config '{key}' is already defined");
                return;
            }

            bool isFirstNetworkedConfig = _networkedConfigHolders.Count == 0;

            _networkedConfigHolders.Add(key, configHolder);

            Log.Debug($"Registered networked config: '{key}'");

            if (isFirstNetworkedConfig)
            {
                Run.onRunStartGlobal += onRunStartGlobal;
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

            Log.Debug($"Set server override value: '{definition}' = {value}");
        }

        public static void ClearOverrideValue(string definition)
        {
            if (!TryGetConfigByDefinition(definition, out ConfigHolderBase configHolder))
            {
                Log.Error($"Networked config '{definition}' is not defined");
                return;
            }

            configHolder.ClearServerOverrideValue();

            Log.Debug($"Cleared server override value for '{definition}'");
        }
    }
}
