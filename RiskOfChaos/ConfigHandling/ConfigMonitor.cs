using System.Collections.Generic;

namespace RiskOfChaos.ConfigHandling
{
    static class ConfigMonitor
    {
        static readonly HashSet<string> _registeredConfigs = [];

        static string getKey(string section, string key)
        {
            // Illegal config char is used as the separator to guarantee it's unique
            return section + '\\' + key;
        }

        public static bool TryRegisterConfig(string section, string key)
        {
            return _registeredConfigs.Add(getKey(section, key));
        }

        public static bool IsConfigRegistered(string section, string key)
        {
            return _registeredConfigs.Contains(getKey(section, key));
        }
    }
}
