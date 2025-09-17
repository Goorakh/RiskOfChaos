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

        public static void ResolveRequiredComponentTypes(IList<Type> definedComponentTypes)
        {
            if (definedComponentTypes == null || definedComponentTypes.Count == 0)
                return;

            static int insertRequiredComponentsFor(Type type, IList<Type> destination, int index)
            {
                int insertRequiredComponentType(Type requiredComponentType, IList<Type> destination, int index)
                {
                    if (requiredComponentType == null ||
                        requiredComponentType.IsInterface ||
                        requiredComponentType.IsAbstract ||
                        !typeof(Component).IsAssignableFrom(requiredComponentType))
                    {
                        Log.Error($"Invalid required component type. type={type.FullName} requiredType={requiredComponentType?.FullName}");
                        return 0;
                    }

                    int numInsertedComponents = 0;

                    if (!destination.Contains(requiredComponentType))
                    {
                        destination.Insert(index, requiredComponentType);
                        numInsertedComponents++;
                    }

                    numInsertedComponents += insertRequiredComponentsFor(requiredComponentType, destination, index);

                    return numInsertedComponents;
                }

                int numInsertedComponents = 0;

                foreach (RequiredComponentsAttribute requiredComponentsAttribute in type.GetCustomAttributes<RequiredComponentsAttribute>(true))
                {
                    foreach (Type requiredComponentType in requiredComponentsAttribute.RequiredComponentTypes)
                    {
                        numInsertedComponents += insertRequiredComponentType(requiredComponentType, destination, index);
                    }
                }

                foreach (RequireComponent requireComponentAttribute in type.GetCustomAttributes<RequireComponent>(true))
                {
                    if (requireComponentAttribute.m_Type0 != null)
                    {
                        numInsertedComponents += insertRequiredComponentType(requireComponentAttribute.m_Type0, destination, index);
                    }

                    if (requireComponentAttribute.m_Type1 != null)
                    {
                        numInsertedComponents += insertRequiredComponentType(requireComponentAttribute.m_Type1, destination, index);
                    }

                    if (requireComponentAttribute.m_Type2 != null)
                    {
                        numInsertedComponents += insertRequiredComponentType(requireComponentAttribute.m_Type2, destination, index);
                    }
                }

                return numInsertedComponents;
            }

#if DEBUG
            Type[] baseComponentTypes = [.. definedComponentTypes];
#endif

            for (int i = 0; i < definedComponentTypes.Count; i++)
            {
                int numInsertedComponents = insertRequiredComponentsFor(definedComponentTypes[i], definedComponentTypes, i);
                i += numInsertedComponents;
            }

#if DEBUG
            if (!definedComponentTypes.SequenceEqual(baseComponentTypes))
            {
                Log.Debug($"""
                    Resolved required component types:
                    [{string.Join<Type>(", ", baseComponentTypes)}] to:
                    [{string.Join(", ", definedComponentTypes)}]
                    """);
            }
#endif
        }
    }
}
