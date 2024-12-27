using HarmonyLib;
using HG.Reflection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace RiskOfChaos.Content
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class PrefabInitializerAttribute : SearchableAttribute
    {
        public new MethodInfo target => base.target as MethodInfo;

        public static IEnumerator RunPrefabInitializers(GameObject prefab)
        {
            List<PrefabInitializerAttribute> attributes = [];
            GetInstances(attributes);

            foreach (MonoBehaviour component in prefab.GetComponentsInChildren<MonoBehaviour>(true))
            {
                Type componentType = component.GetType();

                foreach (PrefabInitializerAttribute attribute in attributes)
                {
                    if (attribute.target.DeclaringType.IsAssignableFrom(componentType))
                    {
                        ParameterInfo[] parameterInfos = attribute.target.GetParameters();
                        object[] parameters = new object[parameterInfos.Length];
                        for (int i = 0; i < parameterInfos.Length; i++)
                        {
                            Type parameterType = parameterInfos[i].ParameterType;
                            if (parameterType == typeof(GameObject))
                            {
                                parameters[i] = component.gameObject;
                            }
                        }

                        object returnValue = attribute.target.Invoke(null, parameters);

                        IEnumerator enumerator = null;
                        if (returnValue is IEnumerator enumeratorValue)
                        {
                            enumerator = enumeratorValue;
                        }
                        else if (returnValue is IEnumerable enumerableValue)
                        {
                            enumerator = enumerableValue.GetEnumerator();
                        }

                        if (enumerator != null)
                        {
                            yield return enumerator;
                        }
                        else if (returnValue != null)
                        {
                            Log.Error($"Unknown return type for prefab intializer {attribute.target.FullDescription()}");
                        }

                        yield return null;
                    }
                }
            }
        }
    }
}
