using HarmonyLib;
using RiskOfChaos.ConfigHandling;
using System;
using System.Reflection;

namespace RiskOfChaos.EffectHandling.EffectClassAttributes.Data
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class EffectConfigAttribute : InitEffectMemberAttribute
    {
        public override InitializationPriority Priority => InitializationPriority.EffectCatalogInitialized;

        public override void ApplyTo(MemberInfo member, ChaosEffectInfo effectInfo)
        {
            switch (getConfigInstance(member))
            {
                case ConfigHolderBase configHolder:
                    configHolder.Bind(effectInfo);
                    break;
                case IConfigProvider configProvider:
                    configProvider.GetConfigs().Do(c => c.Bind(effectInfo));
                    break;
                default:
                    Log.Error($"{member.DeclaringType.FullName}.{member.Name} is not a valid config type");
                    break;
            }
        }

        static object getConfigInstance(MemberInfo member)
        {
            return member switch
            {
                FieldInfo field => field.GetValue(null),
                PropertyInfo property when property.GetMethod is not null => property.GetValue(null),
                _ => throw new NotImplementedException($"Attribute applied to invalid MemberInfo: {member.MemberType} ({member.DeclaringType.FullName}.{member.Name})"),
            };
        }
    }
}
