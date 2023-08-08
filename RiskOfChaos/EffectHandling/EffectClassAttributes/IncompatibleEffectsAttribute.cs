using System;

namespace RiskOfChaos.EffectHandling.EffectClassAttributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public sealed class IncompatibleEffectsAttribute : Attribute
    {
        public readonly Type[] IncompatibleEffectTypes;

        public IncompatibleEffectsAttribute(params Type[] incompatibleEffectTypes)
        {
            IncompatibleEffectTypes = incompatibleEffectTypes;
        }
    }
}
