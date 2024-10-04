using RiskOfChaos.Utilities.Extensions;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace RiskOfChaos.EffectHandling.EffectClassAttributes
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

            List<Type> requiredComponentTypes = new List<Type>(definedComponentTypes);

            int findRequiredComponentIndex(Type requiredComponentType)
            {
                return requiredComponentTypes.FindIndex(requiredComponentType.IsAssignableFrom);
            }

            for (int i = 0; i < requiredComponentTypes.Count; i++)
            {
                void insertRequiredComponents(Type type)
                {
                    void handleRequiredComponentType(Type requiredComponentType)
                    {
                        insertRequiredComponents(requiredComponentType);

                        int index = findRequiredComponentIndex(requiredComponentType);
                        if (index != -1)
                        {
                            if (i < index)
                            {
                                requiredComponentTypes.Insert(i, requiredComponentTypes.GetAndRemoveAt(index));
                            }
                        }
                        else
                        {
                            requiredComponentTypes.Insert(i, requiredComponentType);
                            i++;
                        }
                    }

                    foreach (RequiredComponentsAttribute requiredComponentsAttribute in type.GetCustomAttributes<RequiredComponentsAttribute>(true))
                    {
                        foreach (Type requiredComponentType in requiredComponentsAttribute.RequiredComponentTypes)
                        {
                            handleRequiredComponentType(requiredComponentType);
                        }
                    }

                    foreach (RequireComponent requireComponentAttribute in type.GetCustomAttributes<RequireComponent>(true))
                    {
                        if (requireComponentAttribute.m_Type0 != null)
                            handleRequiredComponentType(requireComponentAttribute.m_Type0);

                        if (requireComponentAttribute.m_Type1 != null)
                            handleRequiredComponentType(requireComponentAttribute.m_Type1);

                        if (requireComponentAttribute.m_Type2 != null)
                            handleRequiredComponentType(requireComponentAttribute.m_Type2);
                    }
                }

                insertRequiredComponents(requiredComponentTypes[i]);
            }

            return requiredComponentTypes.ToArray();
        }
    }
}
