using System;

namespace RiskOfChaos.EffectHandling
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class EffectCanActivateAttribute : Attribute
    {
    }
}