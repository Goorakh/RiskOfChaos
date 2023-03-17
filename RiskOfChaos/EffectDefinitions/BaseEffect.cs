using RiskOfChaos.EffectHandling;
using RiskOfOptions.Options;
using System.Runtime.CompilerServices;

namespace RiskOfChaos.EffectDefinitions
{
    public abstract class BaseEffect
    {
        public Xoroshiro128Plus RNG;

        public abstract void OnStart();

        protected static bool tryGetConfigSectionName(string effectIdentifier, out string configSectionName)
        {
            configSectionName = ChaosEffectCatalog.GetConfigSectionName(effectIdentifier);
            if (string.IsNullOrEmpty(configSectionName))
            {
                Log.Error($"null or empty config section name for {effectIdentifier}");
                configSectionName = null;
                return false;
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void addConfigOption(BaseOption baseOption)
        {
            ChaosEffectCatalog.AddEffectConfigOption(baseOption);
        }
    }
}
