using HG.Reflection;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace RiskOfChaos.Content
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    internal sealed class ContentInitializerAttribute : SearchableAttribute
    {
        public static List<MethodInfo> GetContentInitializers()
        {
            List<SearchableAttribute> attributes = GetInstances<ContentInitializerAttribute>();
            List<MethodInfo> initializerMethods = new List<MethodInfo>(attributes.Count);

            foreach (SearchableAttribute attribute in attributes)
            {
                initializerMethods.Add((MethodInfo)attribute.target);
            }

            return initializerMethods;
        }
    }
}
