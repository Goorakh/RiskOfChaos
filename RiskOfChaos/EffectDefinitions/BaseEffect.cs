using RiskOfChaos.EffectHandling;
using RiskOfOptions.Options;
using System.Runtime.CompilerServices;

namespace RiskOfChaos.EffectDefinitions
{
    public abstract class BaseEffect
    {
        public Xoroshiro128Plus RNG;

        public abstract void OnStart();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static void addConfigOption(BaseOption baseOption)
        {
            ChaosEffectCatalog.AddEffectConfigOption(baseOption);
        }
    }
}
