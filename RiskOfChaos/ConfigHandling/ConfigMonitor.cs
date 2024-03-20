using System.Collections.Generic;

namespace RiskOfChaos.ConfigHandling
{
    static class ConfigMonitor
    {
        static readonly Dictionary<string, ConfigHolderBase> _registeredConfigs = [];

        public static ICollection<ConfigHolderBase> AllConfigs => _registeredConfigs.Values;

        static string getKey(string section, string key)
        {
            // Illegal config char is used as the separator to guarantee it's unique
            return section + '\\' + key;
        }

        public static bool TryRegisterConfig(string section, string key, ConfigHolderBase instance)
        {
            string configKey = getKey(section, key);

            if (_registeredConfigs.ContainsKey(configKey))
                return false;

            _registeredConfigs.Add(getKey(section, key), instance);
            return true;
        }

        public static bool IsConfigRegistered(string section, string key)
        {
            return _registeredConfigs.ContainsKey(getKey(section, key));
        }
    }
}
