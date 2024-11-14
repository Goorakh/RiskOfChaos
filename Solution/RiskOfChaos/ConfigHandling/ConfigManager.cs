using RoR2;
using System.Collections.Generic;

namespace RiskOfChaos.ConfigHandling
{
    static class ConfigManager
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

        [ConCommand(commandName = "roc_delete_config", helpText = "Resets all Risk of Chaos config settings to their default values")]
        static void CCRestoreAllConfigs(ConCommandArgs args)
        {
            foreach (ConfigHolderBase config in AllConfigs)
            {
                if (config.Entry.Definition.Section == Configs.Metadata.SECTION_NAME)
                    continue;

#if DEBUG
                if (!config.IsDefaultValue)
                {
                    Log.Debug($"Reset config value: {config.Entry.Definition}");
                }
#endif

                config.LocalBoxedValue = config.Entry.DefaultValue;
            }
        }
    }
}
