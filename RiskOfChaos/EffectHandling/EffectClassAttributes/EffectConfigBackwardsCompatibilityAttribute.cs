using System;

namespace RiskOfChaos.EffectHandling.EffectClassAttributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class EffectConfigBackwardsCompatibilityAttribute : Attribute
    {
        public readonly string[] ConfigSectionNames;

        public EffectConfigBackwardsCompatibilityAttribute(params string[] configSectionNames)
        {
            ConfigSectionNames = configSectionNames;
        }
    }
}
