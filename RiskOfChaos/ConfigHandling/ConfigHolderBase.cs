using BepInEx.Configuration;
using RiskOfChaos.EffectHandling;

namespace RiskOfChaos.ConfigHandling
{
    public abstract class ConfigHolderBase
    {
        public abstract void Bind(ChaosEffectInfo effectInfo);

        public abstract void Bind(ConfigFile file, string section, string modGuid = null, string modName = null);
    }
}
