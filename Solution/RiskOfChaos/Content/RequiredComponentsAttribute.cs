using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace RiskOfChaos.Content
{
    // Unity for some reason doesn't respect [RequireComponent] where the required component type is not in the base assemblies, or something, idk.
    // Point is, fuck you Unity, there is no reason I should have to do this.

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class RequiredComponentsAttribute : Attribute
    {
        public readonly Type[] RequiredComponentTypes;

        public RequiredComponentsAttribute(params Type[] requiredComponentTypes)
        {
            RequiredComponentTypes = requiredComponentTypes;
        }

        public static Type[] ResolveRequiredComponentTypes(Type[] definedComponentTypes)
        {
            if (definedComponentTypes == null || definedComponentTypes.Length == 0)
                return definedComponentTypes;

            static void addRequiredComponentsFor(Type type, IList<Type> destination)
            {
                void addRequiredComponentType(Type requiredComponentType, IList<Type> destination)
                {
                    if (requiredComponentType == null ||
                        requiredComponentType.IsInterface ||
                        requiredComponentType.IsAbstract ||
                        !typeof(Component).IsAssignableFrom(requiredComponentType))
                    {
                        Log.Error($"Invalid required component type. type={type.FullName} requiredType={requiredComponentType?.FullName}");
                        return;
                    }

                    addRequiredComponentsFor(requiredComponentType, destination);
                    destination.Add(requiredComponentType);
                }

                foreach (RequiredComponentsAttribute requiredComponentsAttribute in type.GetCustomAttributes<RequiredComponentsAttribute>(true))
                {
                    foreach (Type requiredComponentType in requiredComponentsAttribute.RequiredComponentTypes)
                    {
                        addRequiredComponentType(requiredComponentType, destination);
                    }
                }

                foreach (RequireComponent requireComponentAttribute in type.GetCustomAttributes<RequireComponent>(true))
                {
                    if (requireComponentAttribute.m_Type0 != null)
                        addRequiredComponentType(requireComponentAttribute.m_Type0, destination);

                    if (requireComponentAttribute.m_Type1 != null)
                        addRequiredComponentType(requireComponentAttribute.m_Type1, destination);

                    if (requireComponentAttribute.m_Type2 != null)
                        addRequiredComponentType(requireComponentAttribute.m_Type2, destination);
                }
            }

            List<Type> allRequiredComponentTypes = new List<Type>(definedComponentTypes.Length * 2);
            foreach (Type componentType in definedComponentTypes)
            {
                addRequiredComponentsFor(componentType, allRequiredComponentTypes);
                allRequiredComponentTypes.Add(componentType);
            }

            for (int i = 0; i < allRequiredComponentTypes.Count; i++)
            {
                Type pinnedComponentType = allRequiredComponentTypes[i];

                for (int j = allRequiredComponentTypes.Count - 1; j > i; j--)
                {
                    Type componentType = allRequiredComponentTypes[j];

                    if (componentType == pinnedComponentType)
                    {
                        allRequiredComponentTypes.RemoveAt(j);
                    }
                }
            }

#if DEBUG
            if (!allRequiredComponentTypes.SequenceEqual(definedComponentTypes))
            {
                Log.Debug($"""
                    Resolved required component types:
                    [{string.Join<Type>(", ", definedComponentTypes)}] to:
                    [{string.Join(", ", allRequiredComponentTypes)}]
                    """);
            }
#endif

            return allRequiredComponentTypes.ToArray();
        }
    }
}
