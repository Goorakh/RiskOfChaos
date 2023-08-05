using System;
using System.Reflection;

namespace RiskOfChaos.EffectHandling.EffectClassAttributes.Data
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public sealed class InitEffectInfoAttribute : InitEffectMemberAttribute
    {
        public override InitializationPriority Priority => InitializationPriority.EffectInfoCreation;

        public override void ApplyTo(MemberInfo member, ChaosEffectInfo effectInfo)
        {
            if (member is FieldInfo field)
            {
                field.SetValue(null, effectInfo);
            }
            else if (member is PropertyInfo property)
            {
                if (property.SetMethod == null)
                {
                    Log.Error("Attribute applied to property without a setter");
                }
                else
                {
                    property.SetValue(null, effectInfo);
                }
            }
        }
    }
}
