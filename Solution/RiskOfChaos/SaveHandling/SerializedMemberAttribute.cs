using System;
using System.Reflection;

namespace RiskOfChaos.SaveHandling
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class SerializedMemberAttribute : Attribute
    {
        public readonly string OverrideName;

        public SerializedMemberAttribute() : this(string.Empty)
        {
        }

        public SerializedMemberAttribute(string overrideName)
        {
            OverrideName = overrideName;
        }

        public string GetName(MemberInfo memberInfo)
        {
            if (!string.IsNullOrWhiteSpace(OverrideName))
                return OverrideName;

            return memberInfo.Name;
        }
    }
}
