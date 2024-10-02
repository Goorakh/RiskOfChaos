using System;
using System.Reflection;

namespace RiskOfChaos.EffectHandling.EffectClassAttributes.Data
{
    public abstract class InitEffectMemberAttribute : Attribute
    {
        public enum InitializationPriority : byte
        {
            EffectInfoCreation,
            EffectCatalogInitialized
        }

        public abstract InitializationPriority Priority { get; }

        public abstract void ApplyTo(MemberInfo member, ChaosEffectInfo effectInfo);
    }
}
