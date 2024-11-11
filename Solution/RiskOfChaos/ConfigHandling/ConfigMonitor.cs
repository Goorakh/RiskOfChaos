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
            return section + @"\" + key;
        }

        public static bool TryRegisterConfig(string section, string key, ConfigHolderBase instance)
        {
            return _registeredConfigs.TryAdd(getKey(section, key), instance);
        }

        public static bool IsConfigRegistered(string section, string key)
        {
            return _registeredConfigs.ContainsKey(getKey(section, key));
        }
    }
}
