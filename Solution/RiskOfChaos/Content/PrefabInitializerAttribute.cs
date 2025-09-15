using HarmonyLib;
using HG;
using HG.Coroutines;
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

        public static IEnumerator RunPrefabInitializers(ExtendedContentPack contentPack, IProgress<float> progressReceiver)
        {
            List<PrefabInitializerAttribute> attributes = [];
            GetInstances(attributes);

            ParallelProgressCoroutine initializerCoroutines = new ParallelProgressCoroutine(progressReceiver);

            foreach (GameObject prefab in contentPack.prefabs)
            {
                foreach (MonoBehaviour component in prefab.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    Type componentType = component.GetType();

                    foreach (PrefabInitializerAttribute attribute in attributes)
                    {
                        if (attribute.target.DeclaringType.IsAssignableFrom(componentType))
                        {
                            ReadableProgress<float> prefabInitializerProgress = new ReadableProgress<float>();
                            PrefabInitializerArgs prefabInitializerArgs = new PrefabInitializerArgs(prefab, prefabInitializerProgress);

                            object returnValue = attribute.target.Invoke(null, [prefabInitializerArgs]);

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
                                initializerCoroutines.Add(enumerator, prefabInitializerProgress);
                            }
                            else if (returnValue != null)
                            {
                                Log.Error($"Unknown return type for prefab intializer {attribute.target.FullDescription()}");
                            }
                        }
                    }
                }
            }

            return initializerCoroutines;
        }

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
