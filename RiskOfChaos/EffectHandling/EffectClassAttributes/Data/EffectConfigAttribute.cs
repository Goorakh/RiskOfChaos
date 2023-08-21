using RiskOfChaos.ConfigHandling;
using System;
using System.Reflection;

namespace RiskOfChaos.EffectHandling.EffectClassAttributes.Data
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class EffectConfigAttribute : InitEffectMemberAttribute
    {
        public override InitializationPriority Priority => InitializationPriority.EffectCatalogInitialized;

        public override void ApplyTo(MemberInfo member, ChaosEffectInfo effectInfo)
        {
            if (getConfigInstance(member) is ConfigHolderBase configHolder)
            {
                configHolder.Bind(effectInfo);
            }
            else
            {
                Log.Error($"{member.DeclaringType.FullName}.{member.Name} is not a valid config type");
            }
        }

        static object getConfigInstance(MemberInfo member)
        {
            if (member is FieldInfo field)
            {
                return field.GetValue(null);
            }
            else if (member is PropertyInfo property)
            {
                if (property.GetMethod == null)
                {
                    Log.Error("Attribute applied to property without a getter");
                }
                else
                {
                    return property.GetValue(null);
                }
            }
            else
            {
                Log.Error($"Attribute applied to invalid MemberInfo: {member.MemberType} ({member.DeclaringType.FullName}.{member.Name})");
            }

            return null;
        }
    }
}
