using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RiskOfChaos.Utilities.Extensions
{
    public static class ReflectionExtensions
    {
        public static IEnumerable<MethodInfo> GetAllMethodsRecursive(this Type type, BindingFlags flags)
        {
            Type baseType = type.BaseType;
            if (baseType != null)
            {
                foreach (MethodInfo baseMethod in baseType.GetAllMethodsRecursive(flags))
                {
                    yield return baseMethod;
                }
            }

            foreach (MethodInfo method in type.GetMethods(flags))
            {
                yield return method;
            }
        }

        public static IEnumerable<TMember> WithAttribute<TMember, TAttr>(this IEnumerable<TMember> members) where TMember : MemberInfo where TAttr : Attribute
        {
            return members.Where(m => m.GetCustomAttribute<TAttr>() != null);
        }
    }
}
