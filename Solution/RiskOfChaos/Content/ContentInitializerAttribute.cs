using System;
using System.Collections.Generic;
using System.Reflection;

namespace RiskOfChaos.Content
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    internal sealed class ContentInitializerAttribute : Attribute
    {
        public static List<MethodInfo> GetContentInitializers()
        {
            List<MethodInfo> initializerMethods = [];
            foreach (Type type in Assembly.GetExecutingAssembly().GetTypes())
            {
                foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly))
                {
                    ContentInitializerAttribute contentInitializerAttribute = method.GetCustomAttribute<ContentInitializerAttribute>();
                    if (contentInitializerAttribute != null)
                    {
                        initializerMethods.Add(method);
                    }
                }
            }

            return initializerMethods;
        }
    }
}
