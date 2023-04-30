using System;
using System.Collections.Generic;
using System.Text;

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
