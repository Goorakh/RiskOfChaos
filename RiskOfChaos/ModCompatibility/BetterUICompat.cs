using BepInEx;
using BepInEx.Bootstrap;
using BetterUI;
using System.Reflection;

namespace RiskOfChaos.ModCompatibility
{
    public static class BetterUICompat
    {
        public static bool Active => Chainloader.PluginInfos.ContainsKey(BetterUIPlugin.GUID);

        public static Assembly MainAssembly
        {
            get
            {
                if (!Chainloader.PluginInfos.TryGetValue(BetterUIPlugin.GUID, out PluginInfo pluginInfo))
                {
                    Log.Error("Mod not active");
                    return null;
                }

                if (!pluginInfo.Instance)
                {
                    Log.Warning("Mod not loaded?");
                    return null;
                }

                return pluginInfo.Instance.GetType().Assembly;
            }
        }
    }
}
