using System;

namespace RiskOfChaos.EffectHandling.EffectClassAttributes.Methods
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class EffectCanActivateAttribute : Attribute
    {
    }
}