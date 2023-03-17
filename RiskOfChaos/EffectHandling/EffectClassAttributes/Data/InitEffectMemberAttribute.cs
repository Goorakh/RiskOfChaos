using System;
using System.Reflection;

namespace RiskOfChaos.EffectHandling.EffectClassAttributes.Data
{
    public abstract class InitEffectMemberAttribute : Attribute
    {
        public abstract void ApplyTo(MemberInfo member, ChaosEffectInfo effectInfo);
    }
}
