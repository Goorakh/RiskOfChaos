using RiskOfChaos.EffectHandling;
using RiskOfOptions.Options;
using System.Runtime.CompilerServices;

namespace RiskOfChaos.EffectDefinitions
{
    public abstract class BaseEffect
    {
        public Xoroshiro128Plus RNG;

        public abstract void OnStart();

        protected const string ERROR_INVALID_CONFIG_SECTION_NAME = "Failed to bind effect config values: null or empty config section name";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static string getConfigSectionName(string effectIdentifier)
        {
            return ChaosEffectCatalog.GetConfigSectionName(effectIdentifier);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void addConfigOption(BaseOption baseOption)
        {
            ChaosEffectCatalog.AddEffectConfigOption(baseOption);
        }
    }
}
